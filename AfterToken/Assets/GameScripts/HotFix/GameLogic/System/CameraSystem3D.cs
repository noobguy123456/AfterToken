using UnityEngine;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 3D摄像机系统（Hades 风格）。
    /// 负责固定俯视角、有限旋转、平滑跟随、缩放控制。
    /// 移动键只控制玩家移动，摄像头始终跟随玩家。
    /// </summary>
    public class CameraSystem3D : MonoBehaviour
    {
        public static CameraSystem3D Instance { get; private set; }

        [Header("跟随")]
        [SerializeField] private Transform _followTarget;
        [SerializeField] private Vector3 _followOffset = new Vector3(0f, 15f, -10f);

        [Header("缩放")]
        [SerializeField] private float _currentZoom = 15f;
        [SerializeField] private float _minZoom = 5f;
        [SerializeField] private float _maxZoom = 30f;
        [SerializeField] private float _zoomSpeed = 5f;

        [Header("旋转")]
        [SerializeField] private float _yawAngle = 0f;
        [SerializeField] private float _maxRotationAngle = 30f;
        [SerializeField] private float _rotationSpeed = 90f;

        [Header("俯视角度")]
        [SerializeField] private float _pitchAngle = 60f;

        private Camera _mainCamera;

        private void Awake()
        {
            Instance = this;
            _mainCamera = GetComponent<Camera>();
            if (_mainCamera == null)
            {
                _mainCamera = gameObject.AddComponent<Camera>();
            }
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        private void Update()
        {
            HandleMouseInput();
            HandleRotationInput();
            UpdateCameraPosition();
        }

        private void HandleMouseInput()
        {
            // 鼠标滚轮缩放
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _currentZoom -= scroll * _zoomSpeed;
                _currentZoom = Mathf.Clamp(_currentZoom, _minZoom, _maxZoom);
                _followOffset.y = _currentZoom;
            }
        }

        private void HandleRotationInput()
        {
            // 鼠标中键旋转
            if (Input.GetMouseButton(2))
            {
                float mouseX = Input.GetAxis("Mouse X");
                _yawAngle += mouseX * _rotationSpeed * Time.deltaTime;
                _yawAngle = Mathf.Clamp(_yawAngle, -_maxRotationAngle, _maxRotationAngle);
            }

            // Q/E 键旋转
            if (Input.GetKey(KeyCode.Q))
            {
                _yawAngle -= _rotationSpeed * Time.deltaTime;
                _yawAngle = Mathf.Clamp(_yawAngle, -_maxRotationAngle, _maxRotationAngle);
            }
            if (Input.GetKey(KeyCode.E))
            {
                _yawAngle += _rotationSpeed * Time.deltaTime;
                _yawAngle = Mathf.Clamp(_yawAngle, -_maxRotationAngle, _maxRotationAngle);
            }
        }

        private void UpdateCameraPosition()
        {
            if (_followTarget != null)
            {
                // 计算目标位置（跟随目标位置 + 偏移）
                Vector3 targetPos = _followTarget.position + _followOffset;

                // 应用旋转
                targetPos = ApplyRotation(targetPos);

                // 平滑跟随
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);

                // 应用俯视角度
                transform.rotation = Quaternion.Euler(_pitchAngle, _yawAngle, 0f);
            }
        }

        private Vector3 ApplyRotation(Vector3 position)
        {
            // 绕 Y 轴旋转
            Quaternion rotation = Quaternion.Euler(0f, _yawAngle, 0f);
            return rotation * position;
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
            transform.position = new Vector3(position.x, _currentZoom, position.z - 5f);
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
