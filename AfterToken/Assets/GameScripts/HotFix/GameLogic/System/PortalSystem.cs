using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic;
using TEngine;
using UnityEngine;

namespace GameLogic.Portal
{
    /// <summary>
    /// 传送门系统。
    /// 统一管理场景中的所有传送门，处理出现条件、交互提示与场景切换。
    /// </summary>
    public class PortalSystem : MonoBehaviour
    {
        public static PortalSystem Instance { get; private set; }

        private readonly List<PortalEntity> _portals = new List<PortalEntity>();
        private readonly GameEventMgr _eventMgr = new GameEventMgr();

        private PortalEntity _currentPortal;
        private InteractionPromptUI _promptUI;
        private CancellationTokenSource _promptCts;

        /// <summary>
        /// 当前存活敌人数量。
        /// </summary>
        public int AliveEnemyCount { get; private set; }

        /// <summary>
        /// 累计生成敌人数量。
        /// </summary>
        public int TotalSpawnedEnemyCount { get; private set; }

        private void Awake()
        {
            Instance = this;
            RegisterEvents();
        }

        private void Start()
        {
            ScanPortals();
            InitializePortals();
        }

        private void OnDestroy()
        {
            _eventMgr.Clear();
            Instance = null;

            _promptCts?.Cancel();
            _promptCts?.Dispose();
            _promptCts = null;
        }

        private void RegisterEvents()
        {
            _eventMgr.AddEvent<int, int>(IEnemyEvent_Event.OnEnemySpawned, OnEnemySpawned);
            _eventMgr.AddEvent<int>(IEnemyEvent_Event.OnEnemyDied, OnEnemyDied);
            _eventMgr.AddEvent(IBattleInputEvent_Event.OnInteractPressed, OnInteractPressed);
        }

        /// <summary>
        /// 从注册表收集所有 PortalEntity。
        /// </summary>
        private void ScanPortals()
        {
            _portals.Clear();
            _portals.AddRange(PortalRegistry.All);
        }

        /// <summary>
        /// 为每个传送门加载配置并初始化。
        /// </summary>
        private void InitializePortals()
        {
            foreach (var portal in _portals)
            {
                var config = PortalConfigMgr.Instance.Get(portal.ConfigId);
                if (config == null)
                {
                    Log.Warning($"[PortalSystem] Portal config not found: {portal.ConfigId}");
                    continue;
                }

                portal.Initialize(config);

                var condition = CreateCondition(config.spawnCondition);
                if (condition == null || condition.Evaluate(portal))
                {
                    portal.Activate();
                }
            }
        }

        /// <summary>
        /// 玩家进入传送门触发区。
        /// </summary>
        public void OnPlayerEnteredPortal(PortalEntity portal)
        {
            _currentPortal = portal;
            if (portal.IsActivated)
            {
                ShowPrompt(portal.Config?.promptText);
            }
        }

        /// <summary>
        /// 玩家离开传送门触发区。
        /// </summary>
        public void OnPlayerExitedPortal(PortalEntity portal)
        {
            if (_currentPortal == portal)
            {
                _currentPortal = null;
                HidePrompt();
            }
        }

        /// <summary>
        /// 执行传送门转场与场景切换。
        /// </summary>
        public void ExecuteTransition(PortalEntity portal)
        {
            if (portal?.Config == null) return;

            // 死亡判定第二道闸：已死亡的玩家不允许传送（第一道闸在 OnInteractPressed）。
            if (IsPlayerDead())
            {
                return;
            }

            var config = portal.Config;
            if (config.keepPlayerState)
            {
                PortalPlayerState.Save(PlayerSystem.Instance, WeaponSystem.Instance);
            }

            PortalTransitionMgr.PlayAsync(
                config.transitionType,
                config.transitionDuration,
                IsPlayerDead,
                () =>
                {
                    // 转场期间玩家死亡，传送中止：清理已保存的玩家状态，避免污染死亡确认后的重开。
                    PortalPlayerState.Clear();
                },
                () =>
                {
                    switch (config.portalType)
                    {
                        case PortalType.RETURN_TO_LOBBY:
                            GameApp.ChangeProcedure<ProcedureLobby>();
                            break;
                        case PortalType.NEXT_LEVEL:
                            SwitchToNextLevel(config.targetLevelId);
                            break;
                        case PortalType.CUSTOM_SCENE:
                            SwitchToCustomScene(config.targetSceneName);
                            break;
                        default:
                            Log.Error($"[PortalSystem] Unknown portal type: {config.portalType}");
                            break;
                    }
                }).Forget();;
        }

        /// <summary>
        /// 玩家是否已死亡（玩家实体不存在时视为不可传送）。
        /// </summary>
        private static bool IsPlayerDead()
        {
            var player = PlayerSystem.Instance?.GetPlayerEntity();
            return player == null || player.IsDead;
        }

        private void OnEnemySpawned(int enemyId, int configId)
        {
            AliveEnemyCount++;
            TotalSpawnedEnemyCount++;
        }

        private void OnEnemyDied(int enemyId)
        {
            AliveEnemyCount = Mathf.Max(0, AliveEnemyCount - 1);
            EvaluateConditions();
        }

        private void OnInteractPressed()
        {
            // 死亡判定优先：玩家已死亡时必须走死亡确认（Restart / Back to Lobby），
            // 禁止通过传送门绕过，否则会带着 timeScale=0 的暂停状态进入下一场景。
            var player = PlayerSystem.Instance?.GetPlayerEntity();
            if (player == null || player.IsDead)
            {
                return;
            }

            if (_currentPortal != null && _currentPortal.IsActivated)
            {
                _currentPortal.TryInteract();
            }
        }

        /// <summary>
        /// 重新评估所有未激活传送门的出现条件。
        /// </summary>
        private void EvaluateConditions()
        {
            foreach (var portal in _portals)
            {
                if (portal.IsActivated || portal.Config == null) continue;

                var condition = CreateCondition(portal.Config.spawnCondition);
                if (condition != null && condition.Evaluate(portal))
                {
                    portal.Activate();
                    if (_currentPortal == portal)
                    {
                        ShowPrompt(portal.Config.promptText);
                    }
                }
            }
        }

        private IPortalCondition CreateCondition(string conditionType)
        {
            switch (conditionType)
            {
                case PortalSpawnCondition.NONE:
                    return new NoneCondition();
                case PortalSpawnCondition.ALL_ENEMIES_DEFEATED:
                    return new AllEnemiesDefeatedCondition();
                default:
                    Log.Warning($"[PortalSystem] Unsupported spawn condition: {conditionType}");
                    return null;
            }
        }

        private void ShowPrompt(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            ShowPromptAsync(text).Forget();;
        }

        private async UniTaskVoid ShowPromptAsync(string text)
        {
            _promptCts?.Cancel();
            _promptCts?.Dispose();
            _promptCts = new CancellationTokenSource();

            try
            {
                if (_promptUI == null)
                {
                    _promptUI = await GameModule.UI.ShowUIAsyncAwait<InteractionPromptUI>(_promptCts.Token);
                }
                _promptUI?.SetPrompt(text);
            }
            catch (OperationCanceledException)
            {
                // PortalSystem 销毁时取消，忽略异常。
            }
        }

        private void HidePrompt()
        {
            if (_promptUI != null)
            {
                GameModule.UI.CloseUI<InteractionPromptUI>();
                _promptUI = null;
            }
        }

        private void SwitchToNextLevel(int targetLevelId)
        {
            int nextLevelId = targetLevelId > 0 ? targetLevelId : BattleContext.CurrentLevelId + 1;
            var config = LevelConfigMgr.Instance.Get(nextLevelId);
            if (config == null)
            {
                Log.Error($"[PortalSystem] Next level config not found: {nextLevelId}, returning to lobby.");
                GameApp.ChangeProcedure<ProcedureLobby>();
                return;
            }

            BattleContext.CurrentLevelId = nextLevelId;
            GameApp.ChangeProcedure<ProcedureBattle>();
        }

        private void SwitchToCustomScene(string targetSceneName)
        {
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Log.Error("[PortalSystem] Custom scene target is empty, returning to lobby.");
                GameApp.ChangeProcedure<ProcedureLobby>();
                return;
            }

            // 第一阶段：复用战斗流程加载指定场景。
            // 后续若新增专门流程，可在此扩展分支。
            BattleContext.CustomSceneName = targetSceneName;
            GameApp.ChangeProcedure<ProcedureBattle>();
        }
    }
}
