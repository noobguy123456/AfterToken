using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
        }

        private void RegisterEvents()
        {
            _eventMgr.AddEvent<int, int>(IEnemyEvent_Event.OnEnemySpawned, OnEnemySpawned);
            _eventMgr.AddEvent<int>(IEnemyEvent_Event.OnEnemyDied, OnEnemyDied);
            _eventMgr.AddEvent(IBattleInputEvent_Event.OnInteractPressed, OnInteractPressed);
        }

        /// <summary>
        /// 扫描场景中的所有 PortalEntity。
        /// </summary>
        private void ScanPortals()
        {
            var portals = FindObjectsByType<PortalEntity>(FindObjectsSortMode.None);
            _portals.AddRange(portals);
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

            var config = portal.Config;
            if (config.keepPlayerState)
            {
                PortalPlayerState.Save(PlayerSystem.Instance, WeaponSystem.Instance);
            }

            PortalTransitionMgr.PlayAsync(config.transitionType, config.transitionDuration, () =>
            {
                switch (config.portalType)
                {
                    case PortalType.ReturnToLobby:
                        GameApp.ChangeProcedure<ProcedureLobby>();
                        break;
                    case PortalType.NextLevel:
                        SwitchToNextLevel(config.targetLevelId);
                        break;
                    case PortalType.CustomScene:
                        SwitchToCustomScene(config.targetSceneName);
                        break;
                    default:
                        Log.Error($"[PortalSystem] Unknown portal type: {config.portalType}");
                        break;
                }
            }).Forget();
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
                case PortalSpawnCondition.None:
                    return new NoneCondition();
                case PortalSpawnCondition.AllEnemiesDefeated:
                    return new AllEnemiesDefeatedCondition();
                default:
                    Log.Warning($"[PortalSystem] Unsupported spawn condition: {conditionType}");
                    return null;
            }
        }

        private void ShowPrompt(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            ShowPromptAsync(text).Forget();
        }

        private async UniTaskVoid ShowPromptAsync(string text)
        {
            if (_promptUI == null)
            {
                _promptUI = await GameModule.UI.ShowUIAsyncAwait<InteractionPromptUI>();
            }
            _promptUI?.SetPrompt(text);
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
