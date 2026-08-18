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
        private CameraSystem3D _cameraSystem3D;
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
            _battleRoot.AddComponent<LootContainerSystem>();
            _battleRoot.AddComponent<PlayerDeathHandler>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _battleRoot.AddComponent<GameLogic.GM.GMController>();
#endif

            // 设置 PlayerSystem 的生成点
            var playerSystem = _battleRoot.GetComponent<PlayerSystem>();
            if (playerSystem != null)
            {
                var spawnPoint = GameObject.Find("PlayerSpawnPoint");
                if (spawnPoint != null)
                {
                    var spawnPointField = typeof(PlayerSystem).GetField("_spawnPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (spawnPointField != null)
                    {
                        spawnPointField.SetValue(playerSystem, spawnPoint.transform);
                        Log.Info($"[ProcedureBattle] 设置 PlayerSystem 生成点: {spawnPoint.transform.position}");
                    }
                }
                else
                {
                    Log.Warning("[ProcedureBattle] 找不到 PlayerSpawnPoint");
                }
            }

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // 场景相机剔除 UI 层：界面 UI 一律走 Overlay，防止未来 UI 层特效物体被场景相机透视重渲
                mainCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));

                _cameraSystem3D = mainCamera.GetComponent<CameraSystem3D>();
                if (_cameraSystem3D == null)
                {
                    _cameraSystem3D = mainCamera.gameObject.AddComponent<CameraSystem3D>();
                }
            }
            else
            {
                Log.Warning("[ProcedureBattle] 找不到 Main Camera");
            }

            InitBattleBoundary();
        }

        /// <summary>
        /// 用场景 Ground 的渲染边界初始化战斗边界（玩家移动钳制范围）。
        /// </summary>
        private void InitBattleBoundary()
        {
            var ground = GameObject.Find("Ground");
            var groundRenderer = ground != null ? ground.GetComponentInChildren<Renderer>() : null;
            if (groundRenderer != null)
            {
                BattleBoundary.Init(groundRenderer.bounds);
                Log.Info($"[ProcedureBattle] 战斗边界: {groundRenderer.bounds}");
            }
            else
            {
                BattleBoundary.Clear();
                Log.Warning("[ProcedureBattle] 找不到 Ground 的 Renderer，玩家移动将不受边界钳制");
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
            BattleBoundary.Clear();

            if (_cameraSystem3D != null)
            {
                Object.Destroy(_cameraSystem3D);
                _cameraSystem3D = null;
            }

            if (_battleRoot != null)
            {
                SingletonSystem.Release(_battleRoot, null);
                _battleRoot = null;
            }
        }
    }
}
