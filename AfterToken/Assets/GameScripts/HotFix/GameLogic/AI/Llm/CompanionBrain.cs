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
        /// <summary>连续失败达到阈值（TbCompanion.offlineFailThreshold）或未配置 API，全走本地 bark 表，后台探测恢复。</summary>
        Offline,
    }

    /// <summary>
    /// 队友大脑：LLM 链路的调度中枢（纯 C# 类，由 CompanionSystem 每帧 Tick）。
    /// - 轮播六规则：①安全默认轮播设定语言 ②玩家"闭嘴"类指令进入安静模式（自主台词全停，再次主动沟通自动解除）
    ///   ③对话保持期（conversationHold，读表）内暂停轮播 ④敌人发现玩家（ThreatCount 0→>0）播危险预警
    ///   ⑤战斗中不轮播、按 dirHintInterval（读表）节奏报最近威胁方位 ⑥脱战播台词 + postCombatCalm（读表）冷静期后才回安全轮播。
    /// - 意图裁决：LLM intent 白名单校验 + TTL 过期 → 写 Context.PendingRequest（FSM 不知道 LLM 存在）。
    /// - LinkState 三态断联降级，Offline 后台探测恢复。
    /// 战斗类台词（交战/标点/死亡等）不走 LLM，由 <see cref="SayLocal"/> 秒回。
    /// </summary>
    public class CompanionBrain
    {
        /// <summary>进入 Offline 的连续失败次数阈值兜底（TbCompanion.offlineFailThreshold 缺失时用）。</summary>
        private const int OfflineFailThresholdFallback = 3;
        /// <summary>防复读记忆的台词条数。</summary>
        private const int RECENT_LINE_CAP = 3;
        /// <summary>LLM 台词最大长度（契约 ≤60，留宽容截断）。</summary>
        private const int MAX_SAY_LENGTH = 80;
        /// <summary>自由对话（M6）记忆的最大轮数（一轮 = 玩家一句 + 队友一句）。</summary>
        private const int CHAT_HISTORY_ROUNDS = 4;
        /// <summary>自由对话最小发送间隔（秒）：玩家主动对话不套用闲聊的长节奏，只做防抖。</summary>
        private const float ChatMinInterval = 1f;
        /// <summary>对话会话保持时长兜底（秒）（TbCompanion.conversationHold 缺失时用）。</summary>
        private const float ConversationHoldFallback = 30f;
        /// <summary>脱离战斗后的冷静期兜底（秒）（TbCompanion.postCombatCalm 缺失时用）。</summary>
        private const float PostCombatCalmFallback = 20f;
        /// <summary>战斗中方位提示的全局最小间隔兜底（秒）（TbCompanion.dirHintInterval 缺失时用）。</summary>
        private const float DirHintIntervalFallback = 8f;

        private int OfflineFailThreshold =>
            _persona != null && _persona.OfflineFailThreshold > 0
                ? _persona.OfflineFailThreshold : OfflineFailThresholdFallback;
        /// <summary>对话会话保持时长（秒）：最后一条对话后该时长内仍视为"对话中"，暂停随机闲聊。</summary>
        private float ConversationHold =>
            _persona != null && _persona.ConversationHold > 0.1f
                ? _persona.ConversationHold : ConversationHoldFallback;
        /// <summary>脱离战斗后的冷静期（秒）：期间无战斗才恢复安全轮播。</summary>
        private float PostCombatCalm =>
            _persona != null && _persona.PostCombatCalm > 0.1f
                ? _persona.PostCombatCalm : PostCombatCalmFallback;
        /// <summary>战斗中方位提示的全局最小间隔（秒），单方向词条冷却由 bark 表另管。</summary>
        private float DirHintIntervalSec =>
            _persona != null && _persona.DirHintInterval > 0.1f
                ? _persona.DirHintInterval : DirHintIntervalFallback;
        /// <summary>玩家显式移动指令（聊天 follow/hold、G 键跟随）对 LLM 自主决策的压制时长（秒）。</summary>
        public const float PlayerCommandGraceSeconds = 30f;

        /// <summary>
        /// 聊天契约 intent 值（执行侧唯一出处）。
        /// prompt 侧白名单见 <see cref="PromptBuilder.ChatIntentWhitelist"/>，两处取值必须一致。
        /// 与操控通道动作（<see cref="DecisionDriver.ActionFollow"/> 等）是两条独立契约，别混用。
        /// </summary>
        public const string IntentNone = "none";
        public const string IntentFollow = "follow";
        public const string IntentHold = "hold";
        public const string IntentRetreat = "retreat";

        /// <summary>安静指令关键词（中英文，Contains 匹配）。</summary>
        private static readonly string[] QuietKeywords =
        {
            "闭嘴", "安静", "别说话", "别说了", "住口",
            "shut up", "shutup", "be quiet", "quiet", "silence", "stop talking",
        };

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
        /// <summary>最近一次对话活动（玩家发话或收到回复）的时间，用于对话期间暂停随机闲聊。</summary>
        private float _lastChatActivityTime = -999f;

        // ── 轮播调度状态（六条轮播规则）──
        /// <summary>安静模式：玩家明确下达"闭嘴/安静"类指令后置位，停止一切自主台词（轮播+LLM 决策台词），玩家再次主动沟通时自动解除。战术播报（危险预警/方位提示/脱战）不受影响。</summary>
        private bool _quietMode;
        /// <summary>上一帧是否处于战斗（ThreatCount>0），用于危险/脱战沿检测。</summary>
        private bool _wasInCombat;
        /// <summary>最近一次处于战斗的时间，脱战后冷静期由此计时。</summary>
        private float _lastCombatTime = -999f;
        /// <summary>上一次方位提示时间（全局节流）。</summary>
        private float _lastDirHintTime = -999f;

        /// <summary>聊天 UI 是否打开（由 CompanionChatUI OnCreate/OnDestroy 写入；大脑不反向读 UI 类）。</summary>
        public bool ChatUIOpen;

        /// <summary>
        /// 是否处于对话会话中（聊天窗打开，或最后一条对话未满 ConversationHold（读 TbCompanion））。
        /// 对话中暂停安全闲聊（LLM 与本地兜底都停），避免自言自语插进玩家对话。
        /// </summary>
        public bool IsInConversation =>
            ChatUIOpen || Time.time - _lastChatActivityTime < ConversationHold;

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

        /// <summary>LLM 台词出口（聊天/操控频道共用：记防复读 + 发字幕事件）。安静模式下抑制自主台词（玩家对话的回复不走这里，不受影响）。</summary>
        public void SayFromLlm(string text)
        {
            if (_quietMode)
            {
                return;
            }
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
                if (_consecutiveFailures >= OfflineFailThreshold && LinkState != CompanionLinkState.Offline)
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

        /// <summary>队友呼号（字幕显示名），配置缺失回退 Exusiai。</summary>
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

            // ── 轮播调度（六条规则）──
            var companion = _system.Companion;
            if (companion == null || companion.IsDead || companion.Context == null
                || _system.PlayerTransform == null)
            {
                return;
            }

            var ctx = companion.Context;
            bool inCombat = ctx.ThreatCount > 0;

            // 规则4：安全→危险沿（敌人进入追击=发现玩家）播报预警
            if (inCombat && !_wasInCombat)
            {
                SayLocal("danger_alert");
            }
            // 规则6：危险→安全沿播放脱战台词，冷静期由此开始
            if (!inCombat && _wasInCombat)
            {
                SayLocal("combat_end", bypassCooldown: true);
                _lastCombatTime = Time.time;
            }
            _wasInCombat = inCombat;

            // 规则5：战斗中不轮播，只按节奏提示最近威胁方位
            if (inCombat)
            {
                _lastCombatTime = Time.time;
                if (Time.time - _lastDirHintTime >= DirHintIntervalSec)
                {
                    _lastDirHintTime = Time.time;
                    SayDirectionHint(ctx);
                }
                return;
            }

            // 规则6：脱战冷静期内不进入安全轮播
            if (Time.time - _lastCombatTime < PostCombatCalm)
            {
                return;
            }

            // 规则2：安静模式（玩家明确指令，直到玩家再次主动沟通）
            if (_quietMode)
            {
                return;
            }

            // 规则3：对话会话保持期暂停轮播（定时器不走字）
            if (IsInConversation)
            {
                return;
            }

            // 规则1：安全且默认状态 → 轮播设定语言
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
        /// 战斗中方位提示：最近威胁（兜底警戒目标）相对玩家的世界方向，8 方位本地 bark 秒回。
        /// 参照系为世界方向（俯视视角 +Z=屏幕上方=前方），不随玩家面朝变化，保证玩家读得懂。
        /// </summary>
        private void SayDirectionHint(CompanionStateContext ctx)
        {
            var threat = ctx.NearestThreat;
            if (threat == null || threat.IsDead)
            {
                threat = ctx.NearestVisibleEnemy;
            }
            if (threat == null || threat.IsDead)
            {
                return;
            }

            Vector3 delta = threat.transform.position - _system.PlayerTransform.position;
            float angle = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg; // 0=+Z(前) 90=+X(右)
            if (angle < 0f)
            {
                angle += 360f;
            }

            string trigger;
            if (angle < 22.5f || angle >= 337.5f) trigger = "combat_dir_front";
            else if (angle < 67.5f) trigger = "combat_dir_front_right";
            else if (angle < 112.5f) trigger = "combat_dir_right";
            else if (angle < 157.5f) trigger = "combat_dir_back_right";
            else if (angle < 202.5f) trigger = "combat_dir_back";
            else if (angle < 247.5f) trigger = "combat_dir_back_left";
            else if (angle < 292.5f) trigger = "combat_dir_left";
            else trigger = "combat_dir_front_left";
            SayLocal(trigger);
        }

        /// <summary>是否安静指令（"闭嘴/安静/shut up"等，中英文 Contains 匹配）。</summary>
        private static bool IsQuietCommand(string text)
        {
            string t = text.Trim().ToLowerInvariant();
            foreach (var k in QuietKeywords)
            {
                if (t.Contains(k))
                {
                    return true;
                }
            }
            return false;
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

            // 规则2：安静指令最优先识别（战斗中也可生效），本地确认即回，不进 LLM；
            // 玩家之后任何非安静指令的主动沟通自动解除静默
            if (IsQuietCommand(playerText))
            {
                _quietMode = true;
                _lastChatActivityTime = Time.time;
                SayLocal("quiet_ack", bypassCooldown: true);
                return;
            }
            _quietMode = false;

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
            _lastChatActivityTime = Time.time;
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
                        _lastChatActivityTime = Time.time;
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
                else if (_consecutiveFailures < OfflineFailThreshold)
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

            // 请求发出后玩家开聊了或下达了安静指令：这条在途的闲聊台词直接丢弃
            if (trigger == "safe_idle" && (IsInConversation || _quietMode))
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
            if (_consecutiveFailures >= OfflineFailThreshold && LinkState != CompanionLinkState.Offline)
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
                // Anthropic 无 response_format，可能在 JSON 外包一层散文/markdown 代码块，
                // 直解析失败时截取第一个 { 到最后一个 } 再试一次
                var json = TryParseJObject(raw) ?? TryParseJObject(ExtractJsonSubstring(raw));
                if (json == null)
                {
                    return false;
                }
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

        /// <summary>意图裁决：白名单 + TTL 过期作废。仅调整姿态（follow/hold），retreat 映射为驻守收缩。</summary>
        private void TryApplyIntent(string intent, float sendTime)
        {
            if (string.IsNullOrEmpty(intent) || intent == IntentNone)
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

            // 玩家显式指令（聊天里直接下的移动命令）：清掉在途 LLM 指令并进入保护期，
            // 防止下一拍决策用快照坐标把"跟着我"覆盖成"去旧位置站着"
            ctx.ClearLlmDirective();
            ctx.PlayerCommandUntil = Time.time + PlayerCommandGraceSeconds;

            switch (intent)
            {
                case IntentFollow:
                    ctx.FollowEnabled = true;
                    ctx.StanceHold = false;
                    ctx.PendingRequest = new StateTransitionRequest(typeof(CompanionFollowState));
                    break;
                case IntentHold:
                case IntentRetreat: // MVP 无独立撤退建议执行体，映射为驻守（生存撤退由本地硬规则管）
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
            text = SanitizeRichText(text);
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

        // LLM 回复偶尔夹带 <tag> 风格片段（思考标签/情绪标记/JSON 残渣），字幕 TMP 开了 richText
        // 但未知标签会原样显示成 "</>" 这类符号，上字幕/进记忆前统一剥掉
        private static readonly System.Text.RegularExpressions.Regex TagPattern =
            new System.Text.RegularExpressions.Regex("</?[^<>]{0,32}>", System.Text.RegularExpressions.RegexOptions.Compiled);

        private static string SanitizeRichText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            return TagPattern.Replace(text, string.Empty).Trim();
        }

        private void ResetIdleChatTimer()
        {
            float min = _persona != null ? _persona.IdleChatMinInterval : 25f;
            float max = _persona != null ? _persona.IdleChatMaxInterval : 40f;
            _idleChatTimer = UnityEngine.Random.Range(min, max);
        }
    }
}
