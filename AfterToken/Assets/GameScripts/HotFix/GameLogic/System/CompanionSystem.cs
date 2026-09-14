using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic.AI.Llm;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// AI 队友系统：战斗（BattleRoot）与经营（SimulationRoot）场景均挂载。
    /// 负责队友实体的生成/销毁、威胁感知集合维护（订阅敌人状态事件）、
    /// 标点指令转发（订阅 IPingEvent）、跟随开关（G 键）与本地台词输出。
    /// 数值与台词由 TbCompanion / TbCompanionBark 提供；LLM 台词调度见 CompanionBrain（M3）。
    /// </summary>
    public class CompanionSystem : MonoBehaviour
    {
        public static CompanionSystem Instance { get; private set; }

        /// <summary>队友出生偏移兜底值（TbCompanion.spawnOffsetX/Z 缺失时用）。</summary>
        private static readonly Vector3 SpawnOffsetFallback = new Vector3(1.5f, 0f, -1.5f);

        /// <summary>队友出生偏移（相对玩家出生点），读 TbCompanion。</summary>
        private static Vector3 SpawnOffset
        {
            get
            {
                var persona = CompanionConfigMgr.Instance.Get();
                if (persona == null)
                {
                    return SpawnOffsetFallback;
                }
                return new Vector3(persona.SpawnOffsetX, 0f, persona.SpawnOffsetZ);
            }
        }

        private readonly GameEventMgr _eventMgr = new GameEventMgr();
        private readonly HashSet<int> _threats = new HashSet<int>();

        private CompanionEntity _companion;
        private Transform _playerTransform;
        private CompanionBrain _brain;

        // 队友信息交互提示（靠近按交互键打开 CompanionInfoUI），与 NpcSystem 共用 InteractionPromptUI
        private InteractionPromptUI _infoPromptUI;
        private CancellationTokenSource _infoPromptCts;
        private bool _infoPromptShown;

        public CompanionEntity Companion => _companion;

        /// <summary>队友大脑（LLM 链路调度），供调试/M4 设置界面查询链路状态。</summary>
        public CompanionBrain Brain => _brain;

        /// <summary>威胁集合（追击/攻击状态敌人的 InstanceID），供驱动器聚合感知。</summary>
        public HashSet<int> Threats => _threats;

        /// <summary>
        /// LLM 操控通道（M5）是否生效：配置开启 llm 模式且链路未断（Offline 自动回退本地 FSM）。
        /// </summary>
        public static bool IsLlmControlActive =>
            Instance != null && Instance._brain != null
            && Instance._brain.LinkState != CompanionLinkState.Offline
            && LlmConfig.Load().IsLlmControl;

        /// <summary>玩家 Transform：战斗场景走 PlayerSystem，经营场景找 Player 物体（PlayerEntity 已被移除）。</summary>
        public Transform PlayerTransform
        {
            get
            {
                if (_playerTransform == null)
                {
                    var player = PlayerSystem.Instance != null ? PlayerSystem.Instance.GetPlayerEntity() : null;
                    if (player != null)
                    {
                        _playerTransform = player.transform;
                    }
                    else
                    {
                        // 经营场景玩家由 ProcedureSimulation 异步加载（实例名 Player(Clone)，名字查找不可靠），
                        // 直接按经营移动控制器组件定位。
                        var simController = Object.FindObjectOfType<SimulationPlayerController>();
                        if (simController != null)
                        {
                            _playerTransform = simController.transform;
                        }
                    }
                }
                return _playerTransform;
            }
        }

        private void Awake()
        {
            Instance = this;
            _brain = new CompanionBrain(this);

            _eventMgr.AddEvent<int, string, string>(IEnemyEvent_Event.OnEnemyStateChanged, OnEnemyStateChanged);
            _eventMgr.AddEvent<int, int>(IEnemyEvent_Event.OnEnemyDied, OnEnemyDied);
            _eventMgr.AddEvent<PingType, Vector2, int>(IPingEvent_Event.OnPingCreated, OnPingCreated);
            _eventMgr.AddEvent(IPingEvent_Event.OnPingCleared, OnPingCleared);
            _eventMgr.AddEvent<string, string>(ICompanionEvent_Event.OnCompanionStateChanged, OnCompanionStateChanged);
            _eventMgr.AddEvent<int, int, int>(ICompanionAffinityEvent_Event.OnAffinityChanged, OnAffinityChanged);
            _eventMgr.AddEvent<int, int>(ICompanionAffinityEvent_Event.OnAffinityTierUp, OnAffinityTierUp);
        }

        /// <summary>好感度增加：队友头顶粉色飘字 +N ♥。</summary>
        private void OnAffinityChanged(int companionId, int delta, int total)
        {
            if (delta <= 0 || _companion == null)
            {
                return;
            }
            WorldFloatText.Show(Loc.Get("companion.affinity.gain", delta),
                _companion.transform.position, new Color(1f, 0.5f, 0.75f));
        }

        /// <summary>好感度升档：金色飘字 + 升档 bark（若表里有 tier_up 触发）。</summary>
        private void OnAffinityTierUp(int companionId, int newTier)
        {
            if (_companion == null)
            {
                return;
            }
            var tierCfg = CompanionAffinityConfigMgr.Instance.GetTier(newTier);
            string tierName = tierCfg != null ? Loc.Get(tierCfg.NameKey) : $"T{newTier}";
            WorldFloatText.Show(Loc.Get("companion.tierup", tierName),
                _companion.transform.position, new Color(1f, 0.85f, 0.3f));
            _brain?.SayLocal("tier_up", bypassCooldown: true);
        }

        private void OnDestroy()
        {
            _eventMgr.Clear();
            _brain?.Dispose();
            _brain = null;

            _infoPromptCts?.Cancel();
            _infoPromptCts?.Dispose();
            _infoPromptCts = null;
            if (_infoPromptShown)
            {
                _infoPromptShown = false;
                // 提示窗可能被 NpcSystem 接管中，只有未被接管时才由这里关闭
                if (NpcSystem.Instance == null || !NpcSystem.Instance.HasActivePrompt)
                {
                    GameModule.UI.CloseUI<InteractionPromptUI>();
                }
                _infoPromptUI = null;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            // 玩家就绪后懒生成队友（战斗场景 PlayerSystem 异步生成玩家，经营场景同理）
            if (_companion == null && PlayerTransform != null)
            {
                SpawnCompanion(PlayerTransform.position + SpawnOffset);
            }

            // 跟随开关（G）：两个场景通用，故不走战斗 InputSystem；聊天输入框打开时键盘归输入框
            if (!CompanionChatUI.IsOpen && Input.GetKeyDown(KeyBindingSetting.GetKey(KeyBindAction.CompanionFollow)))
            {
                ToggleFollow();
            }

            // 自由对话（M6）：安全状态才开聊；战斗中按键播"没空" bark
            if (!CompanionChatUI.IsOpen && Input.GetKeyDown(KeyBindingSetting.GetKey(KeyBindAction.CompanionChat)))
            {
                TryOpenChat();
            }

            // 队友信息面板入口：靠近队友显示交互提示，按交互键打开
            TickInfoPrompt();

            _brain?.Tick(Time.deltaTime);
        }

        #region 队友信息交互提示

        /// <summary>队友信息提示的交互距离（米）。</summary>
        private const float InfoInteractDistance = 3f;

        /// <summary>
        /// 靠近队友（≤3m）且无菜单 UI/聊天框/对话打开、队友不在交战、附近无 NPC 提示时，
        /// 用 InteractionPromptUI 显示"查看信息"提示，按交互键打开 <see cref="CompanionInfoUI"/>；
        /// 条件不满足时收起提示。
        /// </summary>
        private void TickInfoPrompt()
        {
            bool canShow = _companion != null
                && !_companion.IsDead
                && PlayerTransform != null
                && !CompanionChatUI.IsOpen
                && !GameModule.UI.HasWindow<CompanionInfoUI>()
                && !IsAnyMenuUIOpen()
                && (_companion.Context == null || !_companion.Context.WantsEngage)
                && (DialogueSystem.Instance == null || !DialogueSystem.Instance.IsPlaying)
                && (NpcSystem.Instance == null || !NpcSystem.Instance.HasActivePrompt)
                && Vector3.Distance(PlayerTransform.position, _companion.transform.position) <= InfoInteractDistance;

            if (canShow)
            {
                if (!_infoPromptShown)
                {
                    _infoPromptShown = true;
                    // 词条 ui.interact.companion_info 无占位符，按键名拼在前面（参考 NpcSystem.GetInteractPrompt）
                    string keyName = KeyBindingSetting.GetKeyDisplayName(KeyBindingSetting.GetKey(KeyBindAction.Interact));
                    ShowInfoPromptAsync($"[{keyName}] {Loc.Get("ui.interact.companion_info")}").Forget();
                }

                if (Input.GetKeyDown(KeyBindingSetting.GetKey(KeyBindAction.Interact)))
                {
                    HideInfoPrompt();
                    GameModule.UI.ShowUIAsync<CompanionInfoUI>();
                }
            }
            else if (_infoPromptShown)
            {
                HideInfoPrompt();
            }
        }

        private async UniTaskVoid ShowInfoPromptAsync(string text)
        {
            _infoPromptCts?.Cancel();
            _infoPromptCts?.Dispose();
            _infoPromptCts = new CancellationTokenSource();

            try
            {
                if (_infoPromptUI == null)
                {
                    _infoPromptUI = await GameModule.UI.ShowUIAsyncAwait<InteractionPromptUI>(_infoPromptCts.Token);
                }
                _infoPromptUI?.SetPrompt(text);
            }
            catch (System.OperationCanceledException)
            {
                // CompanionSystem 销毁时取消，忽略异常。
            }
        }

        private void HideInfoPrompt()
        {
            _infoPromptShown = false;

            // 提示窗与 NpcSystem 共用：NPC 提示激活时窗口已被对方接管，由 NpcSystem 负责关闭
            if (NpcSystem.Instance != null && NpcSystem.Instance.HasActivePrompt)
            {
                _infoPromptUI = null;
                return;
            }

            if (_infoPromptUI != null)
            {
                GameModule.UI.CloseUI<InteractionPromptUI>();
                _infoPromptUI = null;
            }
        }

        /// <summary>是否有菜单类窗口打开（开着时不出队友交互提示）。</summary>
        private static bool IsAnyMenuUIOpen()
        {
            return GameModule.UI.HasWindow<SettingsUI>()
                || GameModule.UI.HasWindow<BuildingInfoUI>()
                || GameModule.UI.HasWindow<SkillTreeUI>()
                || GameModule.UI.HasWindow<WarehouseUI>()
                || GameModule.UI.HasWindow<QuestLogUI>()
                || GameModule.UI.HasWindow<QuestBoardUI>()
                || GameModule.UI.HasWindow<BuildingSelectionUI>()
                || GameModule.UI.HasWindow<LobbyUI>()
                || GameModule.UI.HasWindow<BattleBagUI>()
                || GameModule.UI.HasWindow<LootContainerUI>()
                || GameModule.UI.HasWindow<NoteUI>();
        }

        #endregion

        private void SpawnCompanion(Vector3 position)
        {
            var persona = CompanionConfigMgr.Instance.Get();
            int maxHp = persona != null ? persona.MaxHp : 200;
            float moveSpeed = persona != null ? persona.MoveSpeed : 4.2f;
            int weaponId = persona != null ? persona.WeaponConfigId : CompanionEntity.DefaultWeaponConfigId;

            var go = new GameObject("Companion");
            go.transform.position = position;
            _companion = go.AddComponent<CompanionEntity>();
            _companion.Initialize(maxHp, moveSpeed, weaponId);
            Log.Info("[CompanionSystem] 队友已生成");
        }

        /// <summary>
        /// 打开自由对话（M6）：队友存活且不在交战（含主动开火警戒）时才开聊，
        /// 战斗中按键播"没空" bark 作即时反馈。
        /// </summary>
        private void TryOpenChat()
        {
            if (_companion == null || _companion.IsDead || _companion.Context == null) return;

            if (_companion.Context.WantsEngage)
            {
                Say("chat_busy");
                return;
            }
            GameModule.UI.ShowUIAsync<CompanionChatUI>();
        }

        private void ToggleFollow()
        {
            if (_companion == null || _companion.Context == null || _companion.IsDead) return;

            var ctx = _companion.Context;
            ctx.FollowEnabled = !ctx.FollowEnabled;
            // G 键同为玩家显式指令：清 LLM 指令并进保护期（与聊天 follow/hold 同规）
            ctx.ClearLlmDirective();
            ctx.PlayerCommandUntil = Time.time + CompanionBrain.PlayerCommandGraceSeconds;
            if (ctx.FollowEnabled)
            {
                // 切回跟随时清除标点驻守姿态
                ctx.StanceHold = false;
                Say("follow_on");
            }
            else
            {
                Say("follow_off");
            }
            GameEvent.Get<ICompanionEvent>()?.OnCompanionFollowToggled(ctx.FollowEnabled);
        }

        #region 威胁感知

        private void OnEnemyStateChanged(int enemyId, string stateName, string previousStateName)
        {
            if (stateName == "Chase" || stateName == "Attack")
            {
                _threats.Add(enemyId);
            }
            else
            {
                _threats.Remove(enemyId);
            }
        }

        private void OnEnemyDied(int enemyId, int configId)
        {
            _threats.Remove(enemyId);
        }

        private void OnCompanionStateChanged(string stateName, string previousStateName)
        {
            if (stateName == "Engage" && previousStateName != "Engage")
            {
                Say("combat_start");
            }
            else if (stateName == "Retreat" && previousStateName != "Retreat")
            {
                Say("low_hp");
            }
            else if (stateName == "Dead")
            {
                Say("dead", bypassCooldown: true);
            }
        }

        #endregion

        #region 标点指令

        private void OnPingCreated(PingType type, Vector2 pos, int targetId)
        {
            if (_companion == null || _companion.Context == null || _companion.IsDead) return;

            var ctx = _companion.Context;
            ctx.HasPing = true;
            ctx.PingType = type;
            ctx.PingPos = pos;
            ctx.PingTargetId = targetId;
            // 玩家标点永远压过 LLM 指令（硬规矩）
            ctx.ClearLlmDirective();

            switch (type)
            {
                case PingType.Move:
                    Say("ping_move");
                    break;
                case PingType.Loot:
                    Say("ping_loot");
                    break;
                case PingType.Attack:
                    Say("ping_attack");
                    break;
            }
        }

        private void OnPingCleared()
        {
            if (_companion == null || _companion.Context == null) return;
            _companion.Context.ClearPing();
        }

        #endregion

        /// <summary>
        /// 输出队友台词（本地 bark 表即时反馈；安全闲聊等 LLM 台词由 CompanionBrain 调度）。
        /// </summary>
        private void Say(string trigger, bool bypassCooldown = false)
        {
            _brain?.SayLocal(trigger, bypassCooldown);
        }
    }
}
