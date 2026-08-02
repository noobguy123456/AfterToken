using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 模拟经营流程。
    /// </summary>
    public class ProcedureSimulation : GameplayProcedureBase
    {
        private GameObject _simulationRoot;
        private SimulationSystem _simulationSystem;
        private SimulationCameraController _cameraController;
        private GameObject _player;

        protected override UniTaskVoid EnterAsync()
        {
            return LoadSceneWithLoadingAsync("SimulationScene", async ct =>
            {
                Log.Info("[ProcedureSimulation] step1 InitializeSceneContent");
                InitializeSceneContent();
                Log.Info("[ProcedureSimulation] step2 InitializeSimulationSystems");
                InitializeSimulationSystems();
                Log.Info("[ProcedureSimulation] step3 CursorManager");
                CursorManager.Instance?.SetLockMode(GameCursorLockMode.Free);
                CursorManager.Instance?.ForceShowCursor();
                Log.Info("[ProcedureSimulation] step4 SpawnPlayer begin");
                await SpawnPlayerAsync(ct);
                Log.Info("[ProcedureSimulation] step5 ShowUIAsyncAwait begin");
                await GameModule.UI.ShowUIAsyncAwait<SimulationMainUI>();
                Log.Info("[ProcedureSimulation] step6 ShowUI done");
                _simulationSystem?.Enter();
                Log.Info("[ProcedureSimulation] step7 all done");
            });
        }

        protected override void OnLeave(IFsm<IProcedureModule> procedureOwner, bool isShutdown)
        {
            _simulationSystem?.Leave();
            CleanupSimulationSystems();
            base.OnLeave(procedureOwner, isShutdown);
        }

        /// <summary>
        /// 初始化经营场景内容（相机控制）。
        /// 场景内容（地面、光照、玩家生成点）已在场景文件中包含。
        /// </summary>
        private void InitializeSceneContent()
        {
            // 销毁战斗场景的 CameraSystem（避免干扰经营场景相机控制）
            if (CameraSystem.Instance != null)
            {
                Object.Destroy(CameraSystem.Instance);
                Log.Info("[ProcedureSimulation] 销毁战斗场景 CameraSystem");
            }

            // 检查并设置 Main Camera
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Log.Warning("[ProcedureSimulation] 找不到 Main Camera，创建新相机");
                var cameraGo = new GameObject("Main Camera");
                cameraGo.tag = "MainCamera";
                mainCamera = cameraGo.AddComponent<Camera>();
                cameraGo.AddComponent<AudioListener>();
            }

            // 设置相机为 3D 透视相机（经营场景需要 3D 视角）
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = 45f;
            mainCamera.clearFlags = CameraClearFlags.Skybox;

            // 场景相机剔除 UI 层：界面 UI 一律走 Overlay，防止未来 UI 层特效物体被场景相机透视重渲
            mainCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));

            // 设置相机初始位置和角度（俯视视角）
            mainCamera.transform.position = new Vector3(0f, 7f, -5f);
            mainCamera.transform.rotation = Quaternion.Euler(60f, 0f, 0f);

            // 移除战斗跟随相机（若存在），避免与经营相机控制冲突
            var cam3d = mainCamera.GetComponent<CameraSystem3D>();
            if (cam3d != null)
            {
                Object.Destroy(cam3d);
                Log.Info("[ProcedureSimulation] 移除 CameraSystem3D");
            }

            // 添加经营相机控制器（跟随玩家 / 滚轮缩放）
            var cameraController = mainCamera.GetComponent<SimulationCameraController>();
            if (cameraController == null)
            {
                cameraController = mainCamera.gameObject.AddComponent<SimulationCameraController>();
                Log.Info("[ProcedureSimulation] SimulationCameraController 已添加");
            }
            _cameraController = cameraController;

            // 地面染色并生成网格（1 米 1 格），与天空盒区分并提供移动参照（纯色俯视像一片空白）
            var ground = GameObject.Find("Ground");
            var groundRenderer = ground != null ? ground.GetComponent<Renderer>() : null;
            if (groundRenderer != null)
            {
                var groundMaterial = groundRenderer.material;
                groundMaterial.color = Color.white; // 颜色交给纹理，避免叠色
                groundMaterial.mainTexture = CreateGridTexture(
                    new Color(0.45f, 0.55f, 0.45f),
                    new Color(0.34f, 0.44f, 0.34f));
                // 地面实际尺寸（50m），1 格 = 1 米
                var groundSize = groundRenderer.bounds.size;
                groundMaterial.mainTextureScale = new Vector2(groundSize.x, groundSize.z);
            }
        }

        /// <summary>
        /// 运行时生成网格纹理（避免引入额外美术资源）。
        /// </summary>
        private static Texture2D CreateGridTexture(Color cellColor, Color lineColor)
        {
            const int size = 64;
            const int lineWidth = 2;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isLine = x < lineWidth || y < lineWidth;
                    texture.SetPixel(x, y, isLine ? lineColor : cellColor);
                }
            }
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// 加载玩家角色（复用战斗 Player prefab 视觉，移除战斗逻辑组件，挂经营移动控制器）。
        /// </summary>
        private async UniTask SpawnPlayerAsync(System.Threading.CancellationToken ct)
        {
            // 出生点由场景文件提供，缺省用原点
            Vector3 spawnPos = Vector3.zero;
            var spawnPoint = GameObject.Find("PlayerSpawnPoint");
            if (spawnPoint != null)
            {
                spawnPos = spawnPoint.transform.position;
            }

            _player = await GameModule.Resource.LoadGameObjectAsync("Player", spawnPoint != null ? spawnPoint.transform : null, ct);
            if (_player == null)
            {
                Log.Error("[ProcedureSimulation] 加载 Player Prefab 失败");
                return;
            }
            _player.transform.position = spawnPos;

            // 移除战斗逻辑组件（PlayerEntity 依赖战斗系统，经营场景不需要）
            var playerEntity = _player.GetComponent<PlayerEntity>();
            if (playerEntity != null)
            {
                Object.Destroy(playerEntity);
            }

            if (_player.GetComponent<SimulationPlayerController>() == null)
            {
                _player.AddComponent<SimulationPlayerController>();
            }

            // 经营相机比战斗远，放大占位视觉（0.2m → 1m），否则角色只是一个小点；
            // 只改本实例，不影响战斗使用的 prefab。后续换正式角色模型后移除。
            var visual = _player.transform.Find("Visual");
            if (visual != null)
            {
                visual.localScale = Vector3.one * 5f;
                // 占位圆点贴地平放，与地面共面会 z-fighting 导致角色闪烁/消失，抬高 5cm
                visual.localPosition += Vector3.up * 0.05f;
            }

            // 相机跟随玩家（WASD 驱动玩家，相机不再手动平移）
            _cameraController?.SetFollowTarget(_player.transform);
            Log.Info($"[ProcedureSimulation] 玩家已生成，位置: {_player.transform.position}");
        }

        private void InitializeSimulationSystems()
        {
            _simulationRoot = new GameObject("SimulationRoot");
            SingletonSystem.Retain(_simulationRoot, null);
            _simulationSystem = _simulationRoot.AddComponent<SimulationSystem>();

            // 添加 SimulationInputSystem，处理 Esc 键（打开/关闭设置菜单）
            var inputSystem = _simulationRoot.AddComponent<SimulationInputSystem>();
            Log.Info("[ProcedureSimulation] SimulationInputSystem 已添加，处理 Esc 键");

            GrantTestMaterials();
        }

        /// <summary>
        /// 测试期默认物资：所有建筑建造/升级材料补足到堆叠上限，避免测试被"材料不足"卡住。
        /// 正式经济循环（生产/订单产出）接入后移除。
        /// </summary>
        private static void GrantTestMaterials()
        {
            var granted = new System.Collections.Generic.HashSet<int>();
            foreach (var cfg in BuildingConfigMgr.Instance.GetAll())
            {
                GrantItemsToLimit(cfg.BuildCostItems, granted);
                GrantItemsToLimit(cfg.UpgradeCostItems, granted);
            }
        }

        private static void GrantItemsToLimit(System.Collections.Generic.IReadOnlyList<GameConfig.cfg.ItemExchange> items, System.Collections.Generic.HashSet<int> granted)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                // 每种材料只补一次：建造与升级消耗可能引用同一材料
                if (!granted.Add(item.Id))
                {
                    continue;
                }

                int limit = ItemConfigMgr.Instance.GetStackLimit(item.Id);
                int have = Warehouse.GetItemCount(item.Id);
                if (have < limit)
                {
                    Warehouse.TryAdd(item.Id, limit - have);
                    Log.Info($"[ProcedureSimulation] 测试物资：{ItemConfigMgr.Instance.GetName(item.Id)} 补足至上限 x{limit}");
                }
            }
        }

        private void CleanupSimulationSystems()
        {
            if (_simulationRoot != null)
            {
                SingletonSystem.Release(_simulationRoot, null);
                _simulationRoot = null;
                _simulationSystem = null;
            }
        }
    }
}
