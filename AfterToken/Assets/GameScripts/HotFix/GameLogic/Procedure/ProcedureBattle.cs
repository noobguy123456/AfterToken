using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using GameLogic.Navigation;
using GameLogic.Portal;

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
            string sceneName = BattleContext.CustomSceneName;
            if (string.IsNullOrEmpty(sceneName))
            {
                int levelId = BattleContext.CurrentLevelId;
                if (levelId <= 0) levelId = 1;
                _levelConfig = LevelConfigMgr.Instance.Get(levelId);
                if (_levelConfig == null)
                {
                    Log.Error($"[ProcedureBattle] 找不到关卡配置 {levelId}，使用默认关卡 1");
                    _levelConfig = LevelConfigMgr.Instance.Get(1);
                }
                sceneName = _levelConfig?.sceneName ?? "BattleScene";
            }
            else
            {
                BattleContext.CustomSceneName = null;
            }

            return LoadSceneWithLoadingAsync(sceneName, async ct =>
            {
                InitializeBattleSystems();
                ApplyLevelConfig();
                CursorManager.Instance?.SetLockMode(GameCursorLockMode.Locked);
                CursorManager.Instance?.ForceHideCursor();

                await GameModule.UI.ShowUIAsyncAwait<BattleMainUI>();
                await GameModule.UI.ShowUIAsyncAwait<DamageNumberUI>();
                await GameModule.UI.ShowUIAsyncAwait<HitFeedbackUI>();
            });
        }

        protected override void OnLeave(IFsm<IProcedureModule> procedureOwner, bool isShutdown)
        {
            CleanupBattleSystems();
            base.OnLeave(procedureOwner, isShutdown);
        }

        private void InitializeBattleSystems()
        {
            _battleRoot = new GameObject("BattleRoot");
            SingletonSystem.Retain(_battleRoot, null);

            _battleRoot.AddComponent<InputSystem>();
            _battleRoot.AddComponent<PlayerSystem>();
            _battleRoot.AddComponent<WeaponSystem>();
            _battleRoot.AddComponent<AimAssistSystem>();
            _battleRoot.AddComponent<BallisticSystem>();
            _battleRoot.AddComponent<ProjectileSystem>();
            _battleRoot.AddComponent<BattleSystem>();
            _battleRoot.AddComponent<EnemySpawnSystem>();
            _battleRoot.AddComponent<DropSystem>();
            _battleRoot.AddComponent<HitFeedbackSystem>();
            _battleRoot.AddComponent<PoolSystem>();
            _battleRoot.AddComponent<NavigationSystem>();
            _battleRoot.AddComponent<PortalSystem>();
            _battleRoot.AddComponent<PlayerDeathHandler>();

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

            var navSystem = _battleRoot.GetComponent<NavigationSystem>();
            if (navSystem != null)
            {
                navSystem.Initialize(_levelConfig.enemySpawnRadius, playerSystem?.SpawnPosition ?? Vector2.zero);
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
                SingletonSystem.Release(_battleRoot, null);
                _battleRoot = null;
            }
        }
    }
}
