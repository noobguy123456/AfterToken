using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameConfig.cfg;
using Newtonsoft.Json.Linq;
using TEngine;
using UnityEngine;

namespace GameLogic.AI.Llm
{
    /// <summary>
    /// M5 决策节拍器（"AI 开车"）：每隔几秒把战场快报发给 LLM，从固定动作清单里选一个，
    /// 校验合法性后写入 <see cref="CompanionStateContext"/> 的 LLM 指令槽，由现有 FSM 执行。
    /// - 节拍：安全/战斗决策间隔、响应超时、单局限额均读 TbCompanion（缺失用兜底值）。
    /// - 硬规矩：死亡/低血撤退/玩家标点永远优先（驱动器仲裁保证，本类无权绕过）。
    /// - 断联：CompanionSystem.IsLlmControlActive 为 false 时本驱动器空转，行为=纯 FSM。
    /// - 遥测：每次决策追加一行 Logs/companion_control.csv；GM `companion stats` 看汇总。
    /// 与聊天通道（CompanionBrain 安全闲聊）分开计时、分开限额，互不挤占。
    /// </summary>
    public class DecisionDriver
    {
        // ── 节拍与限额（TbCompanion 字段缺失/非正数时用兜底值）──
        private const float SafeIntervalFallback = 8f;
        private const float CombatIntervalFallback = 3f;
        private const float DecisionTimeoutFallback = 3f;
        private const int ControlBudgetFallback = 120;
        /// <summary>快报中最多列出的敌人/掉落物数量（控 token）。</summary>
        private const int MaxReportEntries = 5;
        /// <summary>LLM 台词最大长度（与 CompanionBrain 一致）。</summary>
        private const int MaxSayLength = 80;

        private float SafeInterval =>
            _brain.Persona != null && _brain.Persona.DecisionIntervalSafe > 0.1f
                ? _brain.Persona.DecisionIntervalSafe : SafeIntervalFallback;
        private float CombatInterval =>
            _brain.Persona != null && _brain.Persona.DecisionIntervalCombat > 0.1f
                ? _brain.Persona.DecisionIntervalCombat : CombatIntervalFallback;
        /// <summary>决策响应超过该时长（秒）直接作废，队友继续手上动作。</summary>
        private float DecisionTimeout =>
            _brain.Persona != null && _brain.Persona.DecisionTimeout > 0.1f
                ? _brain.Persona.DecisionTimeout : DecisionTimeoutFallback;
        /// <summary>操控频道单局限额（与聊天频道 LlmBudgetPerRun 独立）。</summary>
        private int ControlBudgetPerRun =>
            _brain.Persona != null && _brain.Persona.ControlBudgetPerRun > 0
                ? _brain.Persona.ControlBudgetPerRun : ControlBudgetFallback;

        private static readonly string[] ActionWhitelist =
            { "follow", "hold", "move_to", "engage", "loot", "retreat", "extract", "none" };

        private readonly CompanionSystem _system;
        private readonly CompanionBrain _brain;

        private float _timer = 2f; // 开局先稳 2 秒再发第一拍
        private bool _inFlight;
        private int _requestCount;

        // ── 遥测 ──
        private int _total;
        private int _valid;
        private int _tokensTotal;
        private long _latencyTotalMs;
        private string _csvPath;

        public DecisionDriver(CompanionSystem system, CompanionBrain brain)
        {
            _system = system;
            _brain = brain;
        }

        /// <summary>由 CompanionBrain.Tick 每帧驱动；非 llm 模式/断联时立即返回（零开销）。</summary>
        public void Tick(float dt)
        {
            if (!CompanionSystem.IsLlmControlActive)
            {
                return;
            }
            var companion = _system.Companion;
            if (companion == null || companion.IsDead || companion.Context == null)
            {
                return;
            }

            _timer -= dt;
            if (_timer > 0f || _inFlight || _requestCount >= ControlBudgetPerRun)
            {
                return;
            }

            bool combat = companion.Context.ThreatCount > 0;
            _timer = combat ? CombatInterval : SafeInterval;
            RequestDecision(companion).Forget();
        }

        // ── 决策请求 ──

        private async UniTaskVoid RequestDecision(CompanionEntity companion)
        {
            _inFlight = true;
            _requestCount++;
            float sendTime = Time.time;
            string state = companion.Context.ThreatCount > 0 ? "combat" : "safe";
            try
            {
                var report = CollectReport(companion, state);
                string system = PromptBuilder.BuildControlSystem(_brain.Persona);
                string user = PromptBuilder.BuildControlUser(in report);
                var result = await _brain.Client.ChatAsync(system, user, _brain.Token);

                if (!result.Ok)
                {
                    _brain.NotifyExternalRequestResult(false);
                    RecordTelemetry(state, sendTime, valid: false, action: "request_failed", tokens: 0);
                    return;
                }

                _brain.NotifyExternalRequestResult(true);
                ApplyDecision(result.Text, sendTime, state, result.TokensUsed);
            }
            finally
            {
                _inFlight = false;
            }
        }

        /// <summary>解析契约 → 校验 → 写指令槽。任何一步非法都记遥测丢弃（这正是验证目标要测的数据）。</summary>
        private void ApplyDecision(string rawContent, float sendTime, string state, int tokens)
        {
            if (!TryParseContract(rawContent, out string action, out string target, out string say))
            {
                RecordTelemetry(state, sendTime, valid: false, action: "parse_failed", tokens: tokens);
                return;
            }

            // 响应超时作废（Time.time 与指令 TTL 同一时钟）
            if (Time.time - sendTime > DecisionTimeout)
            {
                RecordTelemetry(state, sendTime, valid: false, action: action + "|stale", tokens: tokens);
                return;
            }

            if (!TryApply(action, target, sendTime))
            {
                RecordTelemetry(state, sendTime, valid: false, action: action + "|invalid", tokens: tokens);
                return;
            }

            RecordTelemetry(state, sendTime, valid: true, action: action, tokens: tokens);
            if (!string.IsNullOrWhiteSpace(say))
            {
                _brain.SayFromLlm(say);
            }
        }

        // ── 校验与执行 ──

        private static bool TryParseContract(string raw, out string action, out string target, out string say)
        {
            action = null;
            target = null;
            say = null;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            try
            {
                // Anthropic 无 response_format，可能外包散文/markdown，直解析失败截取首个 { 到末个 }
                var json = TryParseJObject(raw) ?? TryParseJObject(ExtractJsonSubstring(raw));
                if (json == null)
                {
                    return false;
                }
                action = json["action"]?.ToString()?.Trim().ToLowerInvariant();
                target = json["target"]?.ToString()?.Trim();
                say = json["say"]?.ToString();
                if (say != null && say.Length > MaxSayLength)
                {
                    say = say.Substring(0, MaxSayLength);
                }
                return !string.IsNullOrEmpty(action) && Array.IndexOf(ActionWhitelist, action) >= 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static JObject TryParseJObject(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }
            try
            {
                return JObject.Parse(text);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string ExtractJsonSubstring(string text)
        {
            int start = text.IndexOf('{');
            int end = text.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                return null;
            }
            return text.Substring(start, end - start + 1);
        }

        /// <summary>目标有效性检查 + 写指令槽。返回 false = 非法决策（丢弃）。</summary>
        private bool TryApply(string action, string target, float sendTime)
        {
            var ctx = _system.Companion?.Context;
            if (ctx == null)
            {
                return false;
            }

            float interval = ctx.ThreatCount > 0 ? CombatInterval : SafeInterval;

            switch (action)
            {
                case "none":
                    return true; // 维持现状，不动指令槽

                case "follow":
                    ctx.FollowEnabled = true;
                    ctx.StanceHold = false;
                    SetDirective(ctx, action, 0, Vector2.zero, interval);
                    return true;

                case "hold":
                case "retreat": // MVP 无独立撤退执行体，映射驻守（生存撤退由驱动器硬规则管）
                    ctx.StanceHold = true;
                    SetDirective(ctx, action, 0, Vector2.zero, interval);
                    return true;

                case "move_to":
                    if (!TryParsePos(target, out Vector2 movePos))
                    {
                        return false;
                    }
                    SetDirective(ctx, action, 0, movePos, interval);
                    return true;

                case "engage":
                    if (!int.TryParse(target, NumberStyles.Integer, CultureInfo.InvariantCulture, out int enemyId)
                        || !EnemyRegistry.TryGet(enemyId, out var enemy) || enemy == null || enemy.IsDead)
                    {
                        return false;
                    }
                    SetDirective(ctx, action, enemyId, Vector2.zero, interval);
                    return true;

                case "loot":
                    if (!int.TryParse(target, NumberStyles.Integer, CultureInfo.InvariantCulture, out int lootId))
                    {
                        return false;
                    }
                    var pickup = FindPickup(lootId);
                    if (pickup == null)
                    {
                        return false;
                    }
                    SetDirective(ctx, action, lootId, pickup.transform.position.ToXZ(), interval);
                    return true;

                case "extract":
                    var point = FindExtractionPoint();
                    if (point == null)
                    {
                        return false;
                    }
                    SetDirective(ctx, action, 0, point.transform.position.ToXZ(), interval);
                    return true;

                default:
                    return false;
            }
        }

        private static void SetDirective(CompanionStateContext ctx, string action, int targetId, Vector2 targetPos, float interval)
        {
            ctx.LlmAction = action;
            ctx.LlmTargetId = targetId;
            ctx.LlmTargetPos = targetPos;
            // 指令 TTL = 两拍：下一拍没来就自动作废，防止陈旧指令卡死状态机
            ctx.LlmDirectiveExpire = Time.time + interval * 2f;
        }

        private static bool TryParsePos(string target, out Vector2 pos)
        {
            pos = default;
            if (string.IsNullOrEmpty(target))
            {
                return false;
            }
            // 容许 "(x, z)" 或 "x,z" 两种写法（模型偶尔会带括号）
            var parts = target.Trim().Trim('(', ')').Split(',');
            if (parts.Length != 2)
            {
                return false;
            }
            return float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out pos.x)
                   && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out pos.y);
        }

        private static PickupEntity FindPickup(int instanceId)
        {
            var list = PickupEntity.Instances;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].GetInstanceID() == instanceId)
                {
                    return list[i];
                }
            }
            return null;
        }

        private static ExtractionPointEntity FindExtractionPoint()
        {
            var points = ExtractionPointEntity.Instances;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] != null)
                {
                    return points[i];
                }
            }
            return null;
        }

        // ── 战场快报采集 ──

        private CompanionControlContext CollectReport(CompanionEntity companion, string state)
        {
            var ctx = companion.Context;
            var report = new CompanionControlContext
            {
                GameState = state,
                CompanionPos = companion.transform.position.ToXZ(),
                CompanionHp = companion.Hp,
                CompanionMaxHp = companion.MaxHp,
                CurrentAction = DescribeCurrentAction(ctx),
            };

            var playerTf = _system.PlayerTransform;
            if (playerTf != null)
            {
                report.PlayerPos = playerTf.position.ToXZ();
            }
            if (PlayerSystem.Instance != null)
            {
                report.PlayerHp = PlayerSystem.Instance.CurrentHp;
                report.PlayerMaxHp = PlayerSystem.Instance.MaxHp;
            }

            report.Enemies = CollectEnemies(report.CompanionPos);
            report.Loot = CollectLoot(report.CompanionPos);

            var point = FindExtractionPoint();
            if (point != null)
            {
                report.ExtractionPos = point.transform.position.ToXZ();
            }
            return report;
        }

        /// <summary>快报用：离队友最近的 N 个敌人（含是否在追玩家）。</summary>
        private List<ControlEnemyInfo> CollectEnemies(Vector2 companionPos)
        {
            var all = EnemyRegistry.All;
            if (all == null || all.Count == 0)
            {
                return null;
            }

            var list = new List<ControlEnemyInfo>();
            var threats = _system.Threats;
            foreach (var enemy in all)
            {
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }
                list.Add(new ControlEnemyInfo
                {
                    Id = enemy.GetInstanceID(),
                    Pos = enemy.transform.position.ToXZ(),
                    Hunting = threats != null && threats.Contains(enemy.GetInstanceID()),
                });
            }
            if (list.Count == 0)
            {
                return null;
            }

            list.Sort((a, b) =>
                (a.Pos - companionPos).sqrMagnitude.CompareTo((b.Pos - companionPos).sqrMagnitude));
            if (list.Count > MaxReportEntries)
            {
                list.RemoveRange(MaxReportEntries, list.Count - MaxReportEntries);
            }
            return list;
        }

        private static List<ControlLootInfo> CollectLoot(Vector2 companionPos)
        {
            var pickups = PickupEntity.Instances;
            if (pickups.Count == 0)
            {
                return null;
            }

            var list = new List<ControlLootInfo>();
            for (int i = 0; i < pickups.Count; i++)
            {
                var p = pickups[i];
                if (p == null)
                {
                    continue;
                }
                list.Add(new ControlLootInfo { Id = p.GetInstanceID(), Pos = p.transform.position.ToXZ() });
            }
            if (list.Count == 0)
            {
                return null;
            }

            list.Sort((a, b) =>
                (a.Pos - companionPos).sqrMagnitude.CompareTo((b.Pos - companionPos).sqrMagnitude));
            if (list.Count > MaxReportEntries)
            {
                list.RemoveRange(MaxReportEntries, list.Count - MaxReportEntries);
            }
            return list;
        }

        private static string DescribeCurrentAction(CompanionStateContext ctx)
        {
            if (ctx.HasPing)
            {
                return "player_ping";
            }
            if (ctx.HasLlmDirective)
            {
                return ctx.LlmAction;
            }
            if (ctx.ThreatCount > 0)
            {
                return "engage";
            }
            return ctx.StanceHold ? "hold" : "follow";
        }

        // ── 遥测（M5c）──

        private void RecordTelemetry(string state, float sendTime, bool valid, string action, int tokens)
        {
            long latencyMs = (long)((Time.time - sendTime) * 1000f);
            _total++;
            if (valid)
            {
                _valid++;
            }
            _latencyTotalMs += latencyMs;
            _tokensTotal += tokens;

            try
            {
                if (string.IsNullOrEmpty(_csvPath))
                {
                    _csvPath = Path.Combine(Application.dataPath, "../Logs/companion_control.csv");
                }
                bool needHeader = !File.Exists(_csvPath) || new FileInfo(_csvPath).Length == 0;
                if (needHeader)
                {
                    File.AppendAllText(_csvPath, "timestamp,state,latency_ms,valid,action,tokens\n");
                }
                File.AppendAllText(_csvPath, string.Format(CultureInfo.InvariantCulture,
                    "{0:yyyy-MM-dd HH:mm:ss},{1},{2},{3},{4},{5}\n",
                    DateTime.Now, state, latencyMs, valid ? 1 : 0, action, tokens));
            }
            catch (Exception e)
            {
                Log.Warning($"[DecisionDriver] 遥测写入失败: {e.Message}");
            }
        }

        /// <summary>GM `companion stats` 汇总。</summary>
        public string GetStatsSummary()
        {
            if (_total == 0)
            {
                return "LLM 操控频道尚无决策记录（需要 controlMode=llm 且链路在线）";
            }
            long avgLatency = _latencyTotalMs / _total;
            float validRate = (float)_valid / _total * 100f;
            return string.Format(CultureInfo.InvariantCulture,
                "决策 {0} 次 | 合法率 {1:F1}% ({2}/{0}) | 平均延迟 {3}ms | token 合计 {4} | 日志 Logs/companion_control.csv",
                _total, validRate, _valid, avgLatency, _tokensTotal);
        }
    }
}
