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
        /// <summary>自由对话（M6）记忆的最大轮数（一轮 = 玩家一句 + 队友一句）。</summary>
        private const int CHAT_HISTORY_ROUNDS = 4;
        /// <summary>自由对话最小发送间隔（秒）：玩家主动对话不套用闲聊的长节奏，只做防抖。</summary>
        private const float ChatMinInterval = 1f;

        private readonly CompanionSystem _system;
        private readonly Companion _persona;
        private LlmClient _client;
        private readonly DecisionDriver _decisionDriver;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly List<string> _recentLines = new List<string>(RECENT_LINE_CAP);
        private readonly Dictionary<int, float> _barkLastTime = new Dictionary<int, float>();

        private int _consecutiveFailures;
        private int _requestCount;
        private float _lastRequestTime = -999f;
        private float _idleChatTimer;
        private float _offlineProbeTimer;
        private bool _requestInFlight;

        // ── M6 自由对话通道状态（与闲聊通道独立计数，避免闲聊占用玩家的对话额度）──
        private readonly List<string> _chatHistory = new List<string>(CHAT_HISTORY_ROUNDS * 2);
        private int _chatRequestCount;
        private float _lastChatRequestTime = -999f;
        private bool _chatInFlight;

        /// <summary>玩家发起的对话请求是否正在等 LLM 回复（聊天 UI 显示等待态用）。</summary>
        public bool ChatInFlight => _chatInFlight;

        public CompanionLinkState LinkState { get; private set; }

        public CompanionBrain(CompanionSystem system)
        {
            _system = system;
            _persona = CompanionConfigMgr.Instance.Get();
            _client = new LlmClient(LlmConfig.Load());
            _decisionDriver = new DecisionDriver(system, this);

            // 未配置直接 Offline；已配置先按 Degraded（未验证），首次成功升 Online
            LinkState = _client.IsConfigured ? CompanionLinkState.Degraded : CompanionLinkState.Offline;
            ResetIdleChatTimer();
        }

        // ── M5 操控通道对接口（DecisionDriver 专用）──

        /// <summary>人设配置。</summary>
        public Companion Persona => _persona;
        /// <summary>HTTP 客户端（聊天/操控频道共用）。</summary>
        public LlmClient Client => _client;
        /// <summary>请求取消令牌。</summary>
        public CancellationToken Token => _cts.Token;
        /// <summary>M5 决策节拍器（GM stats 用）。</summary>
        public DecisionDriver DecisionDriver => _decisionDriver;

        /// <summary>LLM 台词出口（聊天/操控频道共用：记防复读 + 发字幕事件）。</summary>
        public void SayFromLlm(string text)
        {
            EmitSay(text);
        }

        /// <summary>
        /// 外部频道（M5 操控）请求结果回报：共享链路成败计数与状态迁移，
        /// 但不触发本地兜底台词（操控频道静默失败）。
        /// </summary>
        public void NotifyExternalRequestResult(bool ok)
        {
            if (ok)
            {
                _consecutiveFailures = 0;
                if (LinkState != CompanionLinkState.Online)
                {
                    LinkState = CompanionLinkState.Online;
                    Log.Info("[CompanionBrain] LLM 链路已恢复 Online（操控频道）");
                }
            }
            else
            {
                _consecutiveFailures++;
                if (_consecutiveFailures >= OFFLINE_FAIL_THRESHOLD && LinkState != CompanionLinkState.Offline)
                {
                    LinkState = CompanionLinkState.Offline;
                    _offlineProbeTimer = 0f;
                    SayLocal("link_lost", bypassCooldown: true);
                }
                else if (_consecutiveFailures >= 1 && LinkState == CompanionLinkState.Online)
                {
                    LinkState = CompanionLinkState.Degraded;
                    SayLocal("link_degraded");
                }
            }
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

        /// <summary>
        /// 重载配置（设置面板保存后调用）：重建 HTTP 客户端并重算链路状态，
        /// 进行中的请求自然结束后走新配置。
        /// </summary>
        public void ReloadConfig()
        {
            _client = new LlmClient(LlmConfig.Load());
            if (!_client.IsConfigured)
            {
                LinkState = CompanionLinkState.Offline;
            }
            else if (LinkState == CompanionLinkState.Offline)
            {
                // 从"未配置"变为"已配置"：置 Degraded 等首次成功升 Online，并立即探测
                LinkState = CompanionLinkState.Degraded;
                _consecutiveFailures = 0;
                _offlineProbeTimer = _persona != null ? _persona.OfflineProbeInterval : 30f;
            }
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

            // M5 操控频道：自检开关与链路状态，非 llm 模式零开销
            _decisionDriver?.Tick(dt);

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

        #region M6 自由对话通道

        /// <summary>
        /// 玩家自由对话入口（CompanionChatUI 调用）。
        /// 走独立计数与节奏（不抢闲聊/探测额度），回复统一走 EmitSay 上字幕；
        /// 回复附带的 intent 走同一套白名单裁决（follow/hold/retreat 只调姿态）。
        /// </summary>
        public void RequestChatReply(string playerText)
        {
            if (string.IsNullOrWhiteSpace(playerText) || _persona == null)
            {
                return;
            }

            // 战斗中拒聊：CompanionSystem 的 T 键入口已挡，这里双保险
            var companion = _system.Companion;
            if (companion == null || companion.IsDead)
            {
                return;
            }
            if (companion.Context != null && companion.Context.WantsEngage)
            {
                SayLocal("chat_busy");
                return;
            }

            // 断联/未配置：本地 bark 明确体现链路状态（人设内"信号干扰"表达）
            if (LinkState == CompanionLinkState.Offline || !_client.IsConfigured)
            {
                SayLocal("link_lost", bypassCooldown: true);
                return;
            }

            // 节奏/额度/在途限制：打满时给即时反馈而不是静默吞掉玩家输入
            if (_chatInFlight || Time.time - _lastChatRequestTime < ChatMinInterval)
            {
                return; // 上一条还在路上，UI 已有等待态
            }
            if (_chatRequestCount >= _persona.LlmBudgetPerRun)
            {
                SayLocal("chat_busy");
                return;
            }

            RequestChatReplyAsync(playerText.Trim()).Forget();
        }

        private async UniTaskVoid RequestChatReplyAsync(string playerText)
        {
            _chatInFlight = true;
            float sendTime = Time.time;
            try
            {
                var ctx = CollectContext("player_chat");
                string system = PromptBuilder.BuildChatSystem(_persona);
                string user = PromptBuilder.BuildChatUser(in ctx, playerText, _chatHistory);
                var result = await _client.ChatAsync(system, user, _cts.Token);
                _chatRequestCount++;
                _lastChatRequestTime = Time.time;

                // 与操控通道一样共享链路成败计数（不触发兜底台词，下面自行处理）
                NotifyExternalRequestResult(result.Ok);

                if (result.Ok)
                {
                    if (TryParseContract(result.Text, out string say, out string intent))
                    {
                        EmitSay(say);
                        TryApplyIntent(intent, sendTime);
                        RememberChat(playerText, say);
                    }
                    else
                    {
                        // 契约解析失败：整包丢弃转本地兜底，保证玩家有回应
                        SayLocal("safe_idle");
                    }
                }
                else if (_consecutiveFailures < OFFLINE_FAIL_THRESHOLD)
                {
                    // 失败但未掉线（NotifyExternalRequestResult 的状态迁移台词可能没触发），补一句即时反馈
                    SayLocal("chat_busy");
                }
            }
            finally
            {
                _chatInFlight = false;
            }
        }

        /// <summary>记录一轮对话（供下轮请求做上下文），超出上限丢最旧的一轮。</summary>
        private void RememberChat(string playerText, string reply)
        {
            _chatHistory.Add("Boss: " + playerText);
            _chatHistory.Add(CompanionName + ": " + reply);
            while (_chatHistory.Count > CHAT_HISTORY_ROUNDS * 2)
            {
                _chatHistory.RemoveAt(0);
            }
        }

        #endregion

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
                // 只有玩家直接对话（M6 player_chat）允许附带姿态指令；
                // 安全闲聊是即兴台词，LLM 常随口带 hold/follow 把队友钉在原地
                if (trigger == "player_chat")
                {
                    TryApplyIntent(intent, sendTime);
                }
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
            else
            {
                // 经营场景无 PlayerSystem：血量未知不应让 LLM 误读为"玩家已倒下"
                ctx.PlayerHp = -1;
                ctx.PlayerMaxHp = -1;
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
