using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameConfig.cfg;
using Newtonsoft.Json.Linq;
using TEngine;
using UnityEngine;

namespace GameLogic.AI.Llm
{
    /// <summary>LLM 链路状态。</summary>
    public enum CompanionLinkState
    {
        /// <summary>最近请求成功，正常 LLM 台词。</summary>
        Online,
        /// <summary>连续失败 1~2 次，台词混入"信号干扰"表达。</summary>
        Degraded,
        /// <summary>连续失败 ≥3 次或未配置 API，全走本地 bark 表，后台探测恢复。</summary>
        Offline,
    }

    /// <summary>
    /// 队友大脑：LLM 链路的调度中枢（纯 C# 类，由 CompanionSystem 每帧 Tick）。
    /// - 台词调度：安全闲聊定时器触发 → Online 问 LLM / 否则本地 bark 兜底 → 统一发 ICompanionEvent.OnCompanionSay。
    /// - 意图裁决：LLM intent 白名单校验 + TTL 过期 → 写 Context.PendingRequest（FSM 不知道 LLM 存在）。
    /// - LinkState 三态断联降级，Offline 后台探测恢复。
    /// 战斗类台词（交战/标点/死亡等）不走 LLM，由 <see cref="SayLocal"/> 秒回。
    /// </summary>
    public class CompanionBrain
    {
        /// <summary>进入 Offline 的连续失败次数阈值。</summary>
        private const int OFFLINE_FAIL_THRESHOLD = 3;
        /// <summary>防复读记忆的台词条数。</summary>
        private const int RECENT_LINE_CAP = 3;
        /// <summary>LLM 台词最大长度（契约 ≤60，留宽容截断）。</summary>
        private const int MAX_SAY_LENGTH = 80;

        private readonly CompanionSystem _system;
        private readonly Companion _persona;
        private readonly LlmClient _client;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly List<string> _recentLines = new List<string>(RECENT_LINE_CAP);
        private readonly Dictionary<int, float> _barkLastTime = new Dictionary<int, float>();

        private int _consecutiveFailures;
        private int _requestCount;
        private float _lastRequestTime = -999f;
        private float _idleChatTimer;
        private float _offlineProbeTimer;
        private bool _requestInFlight;

        public CompanionLinkState LinkState { get; private set; }

        public CompanionBrain(CompanionSystem system)
        {
            _system = system;
            _persona = CompanionConfigMgr.Instance.Get();
            _client = new LlmClient(LlmConfig.Load());

            // 未配置直接 Offline；已配置先按 Degraded（未验证），首次成功升 Online
            LinkState = _client.IsConfigured ? CompanionLinkState.Degraded : CompanionLinkState.Offline;
            ResetIdleChatTimer();
        }

        /// <summary>队友呼号（字幕显示名），配置缺失回退 Rook。</summary>
        public string CompanionName
        {
            get
            {
                if (_persona != null && !string.IsNullOrEmpty(_persona.NameKey))
                {
            return Loc.Get(_persona.NameKey);
                }
                return CompanionEntity.CompanionName;
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        /// <summary>每帧驱动：安全闲聊定时器 + 离线恢复探测。</summary>
        public void Tick(float dt)
        {
            if (_persona == null)
            {
                return;
            }

            if (LinkState == CompanionLinkState.Offline)
            {
                // 后台探测恢复（只在已配置时有意义）
                if (_client.IsConfigured)
                {
                    _offlineProbeTimer += dt;
                    if (_offlineProbeTimer >= _persona.OfflineProbeInterval && !_requestInFlight)
                    {
                        _offlineProbeTimer = 0f;
                        RequestLlmLine("probe", isProbe: true).Forget();
                    }
                }
            }

            // 安全闲聊：仅安全状态（无威胁、队友存活、玩家在场）触发
            var companion = _system.Companion;
            bool safe = companion != null && !companion.IsDead
                        && companion.Context != null && companion.Context.ThreatCount <= 0
                        && _system.PlayerTransform != null;
            if (!safe)
            {
                return;
            }

            _idleChatTimer -= dt;
            if (_idleChatTimer <= 0f)
            {
                ResetIdleChatTimer();
                if (LinkState == CompanionLinkState.Offline)
                {
                    // 离线全走本地表，闲聊不中断
                    SayLocal("safe_idle");
                }
                else
                {
                    RequestLlmLine("safe_idle", isProbe: false).Forget();
                }
            }
        }

        /// <summary>
        /// 本地 bark 台词（战斗/标点/跟随切换等即时反馈入口，永不经过 LLM）。
        /// 按表权重抽取，遵守单条冷却。
        /// </summary>
        public void SayLocal(string trigger, bool bypassCooldown = false)
        {
            var bark = CompanionConfigMgr.Instance.PickBark(trigger);
            if (bark == null)
            {
                return;
            }

            if (!bypassCooldown && bark.Cooldown > 0.01f
                && _barkLastTime.TryGetValue(bark.Id, out float last)
                && Time.time - last < bark.Cooldown)
            {
                return;
            }
            _barkLastTime[bark.Id] = Time.time;

            EmitSay(Loc.Get(bark.TextKey));
        }

        #region LLM 链路

        private async UniTaskVoid RequestLlmLine(string trigger, bool isProbe)
        {
            if (_requestInFlight || !CanSpendRequest())
            {
                // 预算/节奏打满：静默跳过（探测）或本地兜底（闲聊）
                if (!isProbe)
                {
                    SayLocal(trigger);
                }
                return;
            }

            _requestInFlight = true;
            float sendTime = Time.time;
            try
            {
                var ctx = CollectContext(trigger);
                string system = PromptBuilder.BuildSystem(_persona);
                string user = PromptBuilder.BuildUser(in ctx);
                var result = await _client.ChatAsync(system, user, _cts.Token);

                if (result.Ok)
                {
                    OnRequestSuccess(result.Text, sendTime, isProbe, trigger);
                }
                else
                {
                    OnRequestFailure(result.Error, isProbe, trigger);
                }
            }
            finally
            {
                _requestInFlight = false;
            }
        }

        private bool CanSpendRequest()
        {
            if (_persona == null)
            {
                return false;
            }
            if (_requestCount >= _persona.LlmBudgetPerRun)
            {
                return false;
            }
            return Time.time - _lastRequestTime >= _persona.LlmRequestInterval;
        }

        private void OnRequestSuccess(string rawContent, float sendTime, bool isProbe, string trigger)
        {
            _requestCount++;
            _lastRequestTime = Time.time;
            _consecutiveFailures = 0;

            if (LinkState != CompanionLinkState.Online)
            {
                LinkState = CompanionLinkState.Online;
                if (!isProbe)
                {
                    SayLocal("link_recovered", bypassCooldown: true);
                }
            }

            if (isProbe)
            {
                return;
            }

            // 解析输出契约：say 提取失败整包丢弃转本地兜底；intent 校验白名单 + TTL
            if (TryParseContract(rawContent, out string say, out string intent))
            {
                EmitSay(say);
                TryApplyIntent(intent, sendTime);
            }
            else
            {
                SayLocal(trigger);
            }
        }

        private void OnRequestFailure(string error, bool isProbe, string trigger)
        {
            _requestCount++;
            _lastRequestTime = Time.time;
            _consecutiveFailures++;
            Log.Warning($"[CompanionBrain] LLM 请求失败({_consecutiveFailures}): {error}");

            bool enteredOffline = false;
            if (_consecutiveFailures >= OFFLINE_FAIL_THRESHOLD && LinkState != CompanionLinkState.Offline)
            {
                LinkState = CompanionLinkState.Offline;
                _offlineProbeTimer = 0f;
                enteredOffline = true;
            }
            else if (_consecutiveFailures >= 1 && LinkState == CompanionLinkState.Online)
            {
                LinkState = CompanionLinkState.Degraded;
                SayLocal("link_degraded");
            }

            if (!isProbe)
            {
                // 本地兜底台词优先，链路中断台词压在最上层不被顶掉
                SayLocal(trigger);
                if (enteredOffline)
                {
                    SayLocal("link_lost", bypassCooldown: true);
                }
            }
        }

        private static bool TryParseContract(string raw, out string say, out string intent)
        {
            say = null;
            intent = null;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            try
            {
                var json = JObject.Parse(raw);
                say = json["say"]?.ToString();
                intent = json["intent"]?.ToString();
                if (string.IsNullOrWhiteSpace(say))
                {
                    return false;
                }
                if (say.Length > MAX_SAY_LENGTH)
                {
                    say = say.Substring(0, MAX_SAY_LENGTH);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>意图裁决：白名单 + TTL 过期作废。仅调整姿态（follow/hold），retreat 映射为驻守收缩。</summary>
        private void TryApplyIntent(string intent, float sendTime)
        {
            if (string.IsNullOrEmpty(intent) || intent == "none")
            {
                return;
            }
            if (Time.time - sendTime > _persona.IntentTtl)
            {
                return; // LLM 延迟产生的过期意图直接作废
            }

            var companion = _system.Companion;
            var ctx = companion?.Context;
            if (ctx == null || companion.IsDead)
            {
                return;
            }

            switch (intent)
            {
                case "follow":
                    ctx.FollowEnabled = true;
                    ctx.StanceHold = false;
                    ctx.PendingRequest = new StateTransitionRequest(typeof(CompanionFollowState));
                    break;
                case "hold":
                case "retreat": // MVP 无独立撤退建议执行体，映射为驻守（生存撤退由本地硬规则管）
                    ctx.StanceHold = true;
                    ctx.PendingRequest = new StateTransitionRequest(typeof(CompanionHoldState));
                    break;
                // 其他值不在白名单，丢弃
            }
        }

        private CompanionPromptContext CollectContext(string trigger)
        {
            var ctx = new CompanionPromptContext
            {
                Trigger = trigger,
                RecentLines = new List<string>(_recentLines),
            };

            var companion = _system.Companion;
            if (companion != null)
            {
                ctx.CompanionHp = companion.Hp;
                ctx.CompanionMaxHp = companion.MaxHp;
                ctx.ThreatCount = companion.Context?.ThreatCount ?? 0;
            }
            ctx.GameState = ctx.ThreatCount > 0 ? "combat" : "safe";

            if (PlayerSystem.Instance != null)
            {
                ctx.PlayerHp = PlayerSystem.Instance.CurrentHp;
                ctx.PlayerMaxHp = PlayerSystem.Instance.MaxHp;
            }

            return ctx;
        }

        #endregion

        private void EmitSay(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            _recentLines.Add(text);
            if (_recentLines.Count > RECENT_LINE_CAP)
            {
                _recentLines.RemoveAt(0);
            }
            GameEvent.Get<ICompanionEvent>()?.OnCompanionSay(CompanionName, text);
        }

        private void ResetIdleChatTimer()
        {
            float min = _persona != null ? _persona.IdleChatMinInterval : 25f;
            float max = _persona != null ? _persona.IdleChatMaxInterval : 40f;
            _idleChatTimer = UnityEngine.Random.Range(min, max);
        }
    }
}
