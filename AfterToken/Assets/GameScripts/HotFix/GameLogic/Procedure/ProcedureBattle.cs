using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 战斗流程。
    /// </summary>
    public class ProcedureBattle : GameplayProcedureBase
    {
        private GameObject _battleRoot;
        private CameraSystem _cameraSystem;
        private LevelConfig _levelConfig;

        protected override UniTaskVoid EnterAsync()
        {
            int levelId = BattleContext.CurrentLevelId;
            if (levelId <= 0) levelId = 1;
            _levelConfig = LevelConfigMgr.Instance.Get(levelId);
            if (_levelConfig == null)
            {
                Log.Error($"[ProcedureBattle] 找不到关卡配置 {levelId}，使用默认关卡 1");
                _levelConfig = LevelConfigMgr.Instance.Get(1);
            }

            Log.Debug($"[ProcedureBattle] 进入战斗流程，关卡={levelId} 场景={_levelConfig?.sceneName}");
            return LoadSceneWithLoadingAsync(_levelConfig.sceneName, async ct =>
            {
                Log.Debug("[ProcedureBattle] 场景加载完成，初始化战斗系统");
                InitializeBattleSystems();
                ApplyLevelConfig();
                Log.Debug("[ProcedureBattle] 战斗系统初始化完成，隐藏系统光标");
                CursorManager.Instance?.SetLockMode(GameCursorLockMode.Locked);
                CursorManager.Instance?.ForceHideCursor();

                await GameModule.UI.ShowUIAsyncAwait<BattleMainUI>();
                Log.Debug("[ProcedureBattle] BattleMainUI 已打开");
                await GameModule.UI.ShowUIAsyncAwait<DamageNumberUI>();
                Log.Debug("[ProcedureBattle] DamageNumberUI 已打开");
                await GameModule.UI.ShowUIAsyncAwait<HitFeedbackUI>();
                Log.Debug("[ProcedureBattle] HitFeedbackUI 已打开");
            });
        }

        protected override void OnLeave(IFsm<IProcedureModule> procedureOwner, bool isShutdown)
        {
            CleanupBattleSystems();
            base.OnLeave(procedureOwner, isShutdown);
        }

        private void InitializeBattleSystems()
        {
            Log.Debug("[ProcedureBattle] 创建 BattleRoot 并挂载系统组件");
            _battleRoot = new GameObject("BattleRoot");

            _battleRoot.AddComponent<InputSystem>();
            _battleRoot.AddComponent<PlayerSystem>();
            _battleRoot.AddComponent<WeaponSystem>();
            _battleRoot.AddComponent<AimAssistSystem>();
            _battleRoot.AddComponent<BallisticSystem>();
            _battleRoot.AddComponent<ProjectileSystem>();
            _battleRoot.AddComponent<BattleSystem>();
            _battleRoot.AddComponent<EnemySpawnSystem>();
            _battleRoot.AddComponent<HitFeedbackSystem>();
            _battleRoot.AddComponent<PoolSystem>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _battleRoot.AddComponent<GameLogic.GM.GMController>();
#endif

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                _cameraSystem = mainCamera.GetComponent<CameraSystem>();
                if (_cameraSystem == null)
                {
                    _cameraSystem = mainCamera.gameObject.AddComponent<CameraSystem>();
                }
            }
            else
            {
                Log.Warning("[ProcedureBattle] 找不到 Main Camera");
            }
        }

        private void ApplyLevelConfig()
        {
            if (_levelConfig == null || _battleRoot == null) return;

            var playerSystem = _battleRoot.GetComponent<PlayerSystem>();
            if (playerSystem != null)
            {
                playerSystem.SetMaxHp(_levelConfig.playerMaxHp);
            }

            var enemySpawn = _battleRoot.GetComponent<EnemySpawnSystem>();
            if (enemySpawn != null)
            {
                enemySpawn.Initialize(
                    _levelConfig.enemyCount,
                    _levelConfig.enemySpawnRadius,
                    _levelConfig.enemyConfigId,
                    _levelConfig.enemyMaxHp);
            }

            var weaponSystem = _battleRoot.GetComponent<WeaponSystem>();
            if (weaponSystem != null && _levelConfig.defaultWeaponIds != null)
            {
                weaponSystem.SetDefaultWeapons(_levelConfig.defaultWeaponIds);
            }
        }

        private void CleanupBattleSystems()
        {
            PoolSystem.Instance?.ClearAll();

            if (_cameraSystem != null)
            {
                Object.Destroy(_cameraSystem);
                _cameraSystem = null;
            }

            if (_battleRoot != null)
            {
                Object.Destroy(_battleRoot);
                _battleRoot = null;
            }
        }
    }
}
