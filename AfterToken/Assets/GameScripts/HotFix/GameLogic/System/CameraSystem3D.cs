using UnityEngine;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 3D摄像机系统（Hades 风格）。
    /// 固定俯视角纯跟随：移动键只控制玩家移动，摄像头始终跟随玩家。
    /// 战斗内不提供滚轮缩放与 Q/E 旋转（与切武器、交互键冲突），仅保留鼠标中键拖拽微调旋转。
    /// 参数来源于 <see cref="Camera3DConfigMgr"/>（Luban TbCamera3D，缺失时用代码默认值）。
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

        private float _followSmoothTime = 0.08f;
        private Camera _mainCamera;

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
            _followSmoothTime = Mathf.Max(0.01f, config.FollowSmoothTime);

            if (_mainCamera != null && config.Fov > 0f)
            {
                _mainCamera.fieldOfView = config.Fov;
            }
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        private void Update()
        {
            HandleRotationInput();
            UpdateCameraPosition();
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

            // 平滑跟随
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime / _followSmoothTime);

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
    }
}
