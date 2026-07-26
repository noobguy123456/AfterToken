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
        private GameObject _virtualPlayer;

        protected override UniTaskVoid EnterAsync()
        {
            return LoadSceneWithLoadingAsync("SimulationScene", async ct =>
            {
                InitializeSceneContent();
                InitializeSimulationSystems();
                CursorManager.Instance?.SetLockMode(GameCursorLockMode.Free);
                CursorManager.Instance?.ForceShowCursor();
                await GameModule.UI.ShowUIAsyncAwait<SimulationMainUI>();
                _simulationSystem?.Enter();
            });
        }

        protected override void OnLeave(IFsm<IProcedureModule> procedureOwner, bool isShutdown)
        {
            _simulationSystem?.Leave();
            CleanupSimulationSystems();
            base.OnLeave(procedureOwner, isShutdown);
        }

        /// <summary>
        /// 初始化经营场景内容（虚拟玩家、相机跟随）。
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

            // 创建虚拟玩家角色（透明胶囊体，作为摄像机跟随目标）
            _virtualPlayer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _virtualPlayer.name = "VirtualPlayer";
            _virtualPlayer.transform.position = Vector3.zero;
            _virtualPlayer.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            
            // 设置半透明材质（提高可见性）
            var renderer = _virtualPlayer.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(1f, 0f, 0f, 0.5f); // 红色半透明，更容易看到
            }
            
            // 移除碰撞体，避免干扰点击检测
            var collider = _virtualPlayer.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            Log.Info($"[ProcedureSimulation] 虚拟玩家创建成功，位置: {_virtualPlayer.transform.position}");

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
            
            // 设置相机初始位置和角度（俯视视角）
            mainCamera.transform.position = new Vector3(0f, 15f, -10f);
            mainCamera.transform.rotation = Quaternion.Euler(60f, 0f, 0f);

            // 添加 CameraSystem3D
            var cameraSystem3D = mainCamera.GetComponent<CameraSystem3D>();
            if (cameraSystem3D == null)
            {
                cameraSystem3D = mainCamera.gameObject.AddComponent<CameraSystem3D>();
                Log.Info("[ProcedureSimulation] CameraSystem3D 已添加");
            }
            else
            {
                Log.Info("[ProcedureSimulation] CameraSystem3D 已存在");
            }
            
            // 设置跟随目标
            cameraSystem3D.SetFollowTarget(_virtualPlayer.transform);
            Log.Info($"[ProcedureSimulation] 相机跟随目标设置为: {_virtualPlayer.name}");
        }

        private void InitializeSimulationSystems()
        {
            _simulationRoot = new GameObject("SimulationRoot");
            SingletonSystem.Retain(_simulationRoot, null);
            _simulationSystem = _simulationRoot.AddComponent<SimulationSystem>();
            
            // 添加 SimulationInputSystem，处理 Esc 键（打开/关闭设置菜单）
            var inputSystem = _simulationRoot.AddComponent<SimulationInputSystem>();
            Log.Info("[ProcedureSimulation] SimulationInputSystem 已添加，处理 Esc 键");
        }

        private void CleanupSimulationSystems()
        {
            if (_virtualPlayer != null)
            {
                Object.Destroy(_virtualPlayer);
                _virtualPlayer = null;
            }

            if (_simulationRoot != null)
            {
                SingletonSystem.Release(_simulationRoot, null);
                _simulationRoot = null;
                _simulationSystem = null;
            }
        }
    }
}
