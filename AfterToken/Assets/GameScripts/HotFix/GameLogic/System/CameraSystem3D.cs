using UnityEngine;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 3D摄像机系统（Hades 风格）。
    /// 固定俯视角硬跟随：相机与玩家像素级锁定（无平滑阻尼，避免跟随滞后/顿挫感）。
    /// 战斗内不提供滚轮缩放与 Q/E 旋转（与切武器、交互键冲突），仅保留鼠标中键拖拽微调旋转。
    /// 参数来源于 <see cref="Camera3DConfigMgr"/>（Luban TbCamera3D，缺失时用代码默认值；FollowSmoothTime 已废弃不读）。
    /// </summary>
    public class CameraSystem3D : MonoBehaviour
    {
        public static CameraSystem3D Instance { get; private set; }

        [Header("跟随")]
        [SerializeField] private Transform _followTarget;
        [SerializeField] private Vector3 _followOffset = new Vector3(0f, 5f, -3.5f);

        [Header("旋转")]
        [SerializeField] private float _yawAngle = 0f;
        [SerializeField] private float _maxRotationAngle = 30f;
        [SerializeField] private float _rotationSpeed = 90f;

        [Header("俯视角度")]
        [SerializeField] private float _pitchAngle = 60f;

        [Header("狙击镜")]
        [Tooltip("狙击镜渲染纹理边长（正方形），越大镜内画面越清晰")]
        [SerializeField] private int _scopeRenderSize = 1024;

        private Camera _mainCamera;

        // 狙击镜（Duckov 式）：开镜时把一台小 FOV 相机架在"鼠标瞄准的地面点"上方，
        // 渲染到 RenderTexture，由 SniperScopeUI 显示为跟随鼠标的圆形镜窗
        private Camera _scopeCamera;
        private RenderTexture _scopeRenderTexture;
        private bool _scopeActive;

        /// <summary>
        /// 狙击镜渲染纹理（未开镜时为 null）。
        /// </summary>
        public RenderTexture ScopeRenderTexture => _scopeRenderTexture;

        private void Awake()
        {
            Instance = this;
            _mainCamera = GetComponent<Camera>();
            if (_mainCamera == null)
            {
                _mainCamera = gameObject.AddComponent<Camera>();
            }

            ApplyConfig();
        }

        /// <summary>
        /// 从 Luban 配置读取相机参数（配置缺失时保留代码默认值）。
        /// </summary>
        private void ApplyConfig()
        {
            var config = Camera3DConfigMgr.Instance;
            _pitchAngle = config.PitchAngle;
            _followOffset = new Vector3(0f, config.InitialHeight, config.InitialDistance);
            _maxRotationAngle = config.MaxRotationAngle;
            _rotationSpeed = config.RotationSpeed;

            if (_mainCamera != null && config.Fov > 0f)
            {
                _mainCamera.fieldOfView = config.Fov;
            }
        }

        private void OnDestroy()
        {
            Instance = null;
            ReleaseScopeRenderTexture();
        }

        private void LateUpdate()
        {
            // 相机跟随放在 LateUpdate：等本帧所有移动/旋转（FixedUpdate 物理同步、Update 朝向）结束后再取目标位置，避免跟拍抖动
            HandleRotationInput();
            UpdateCameraPosition();
            if (_scopeActive)
            {
                UpdateScopeCamera();
            }
        }

        private void HandleRotationInput()
        {
            // 仅鼠标中键拖拽旋转；Q/E 与战斗交互键冲突，已移除。
            if (Input.GetMouseButton(2))
            {
                float mouseX = Input.GetAxis("Mouse X");
                _yawAngle += mouseX * _rotationSpeed * Time.deltaTime;
                _yawAngle = Mathf.Clamp(_yawAngle, -_maxRotationAngle, _maxRotationAngle);
            }
        }

        private void UpdateCameraPosition()
        {
            if (_followTarget == null)
            {
                return;
            }

            // 旋转只作用于跟随偏移量（绕玩家转），不能绕世界原点旋转整个目标位置
            Vector3 rotatedOffset = Quaternion.Euler(0f, _yawAngle, 0f) * _followOffset;
            Vector3 targetPos = _followTarget.position + rotatedOffset;

            // 硬跟随：相机与玩家像素级锁定，玩家相对屏幕位置恒定，
            // 不会因平滑阻尼产生"摄像机没跟上"的滞后/顿挫感
            transform.position = targetPos;

            // 应用俯视角度
            transform.rotation = Quaternion.Euler(_pitchAngle, _yawAngle, 0f);
        }

        /// <summary>
        /// 设置跟随目标。
        /// </summary>
        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;
            Log.Info($"[CameraSystem3D] 设置跟随目标: {target?.name ?? "null"}");
        }

        /// <summary>
        /// 聚焦到指定位置。
        /// </summary>
        public void FocusOn(Vector3 position)
        {
            transform.position = position + _followOffset;
        }

        /// <summary>
        /// 获取当前主相机。
        /// </summary>
        public Camera GetMainCamera()
        {
            return _mainCamera;
        }

        #region 狙击镜

        /// <summary>
        /// 开关狙击镜。开镜时创建（或复用）狙击镜相机并指定镜内 FOV（越小倍率越高）。
        /// </summary>
        public void SetScopeActive(bool active, float scopeFov = 15f)
        {
            if (active)
            {
                EnsureScopeCamera();
                _scopeCamera.fieldOfView = scopeFov > 0f ? scopeFov : 15f;
                _scopeCamera.enabled = true;
                _scopeActive = true;
            }
            else
            {
                _scopeActive = false;
                if (_scopeCamera != null)
                {
                    _scopeCamera.enabled = false;
                }
            }
        }

        /// <summary>
        /// 懒创建狙击镜相机与渲染纹理。
        /// </summary>
        private void EnsureScopeCamera()
        {
            if (_scopeCamera != null)
            {
                return;
            }

            var go = new GameObject("ScopeCamera");
            _scopeCamera = go.AddComponent<Camera>();
            _scopeCamera.CopyFrom(_mainCamera);
            _scopeCamera.clearFlags = CameraClearFlags.SolidColor;
            _scopeCamera.backgroundColor = new Color(0.05f, 0.05f, 0.08f);

            _scopeRenderTexture = new RenderTexture(_scopeRenderSize, _scopeRenderSize, 24);
            _scopeRenderTexture.Create();
            _scopeCamera.targetTexture = _scopeRenderTexture;
            _scopeCamera.enabled = false;
        }

        /// <summary>
        /// 每帧把狙击镜相机架到鼠标瞄准的地面点上方，姿态与主相机一致（同俯角/偏航）。
        /// </summary>
        private void UpdateScopeCamera()
        {
            if (_scopeCamera == null)
            {
                return;
            }

            Vector3 aimPoint = GetAimGroundPoint();
            // 让视轴精确穿过瞄准点（镜窗中心=子弹落点）：沿视线方向按跟随高度回推相机位置。
            // 不能简单 aim+offset——主相机的 offset 与俯角并不互为中心（视轴落点偏 -0.61m），
            // 小 FOV 下这点偏移足以把瞄准点挤出画面
            Vector3 forward = Quaternion.Euler(_pitchAngle, _yawAngle, 0f) * Vector3.forward;
            float t = _followOffset.y / -forward.y;
            _scopeCamera.transform.position = aimPoint - forward * t;
            _scopeCamera.transform.rotation = Quaternion.Euler(_pitchAngle, _yawAngle, 0f);
        }

        /// <summary>
        /// 鼠标瞄准的地面点：优先取玩家实体的 AimPosition（与子弹落点一致），
        /// 取不到时用主相机射线与 y=0 平面求交兜底。
        /// </summary>
        private Vector3 GetAimGroundPoint()
        {
            if (_followTarget != null)
            {
                var player = _followTarget.GetComponent<PlayerEntity>();
                if (player != null)
                {
                    Vector2 aim = player.AimPosition;
                    return new Vector3(aim.x, 0f, aim.y);
                }
            }

            if (_mainCamera != null)
            {
                Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                var plane = new Plane(Vector3.up, Vector3.zero);
                if (plane.Raycast(ray, out float enter))
                {
                    return ray.GetPoint(enter);
                }
            }
            return _followTarget != null ? _followTarget.position : Vector3.zero;
        }

        /// <summary>
        /// 世界坐标 → 狙击镜视口坐标（0~1，中心 0.5）。
        /// 未开镜或目标在镜相机后方时返回 false。供 SniperScopeUI 把镜内伤害数字定位到镜窗局部坐标。
        /// </summary>
        public bool TryWorldToScopeViewportPoint(Vector3 worldPos, out Vector2 viewportPoint)
        {
            viewportPoint = default;
            if (!_scopeActive || _scopeCamera == null)
            {
                return false;
            }

            Vector3 vp = _scopeCamera.WorldToViewportPoint(worldPos);
            if (vp.z <= 0f)
            {
                return false;
            }

            viewportPoint = new Vector2(vp.x, vp.y);
            return true;
        }

        private void ReleaseScopeRenderTexture()
        {
            if (_scopeRenderTexture != null)
            {
                _scopeRenderTexture.Release();
                Destroy(_scopeRenderTexture);
                _scopeRenderTexture = null;
            }
        }

        #endregion
    }
}
