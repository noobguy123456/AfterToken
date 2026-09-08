using TEngine;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameLogic
{
    /// <summary>
    /// 经营场景相机控制器：跟随目标 + 滚轮缩放 + 手动平移（脱离跟随时）。
    /// 相机参数与战斗场景统一，同源于 Luban TbCamera3D（<see cref="Camera3DConfigMgr"/>）：
    /// 俯仰角/初始高度/初始距离/FOV/缩放范围与战斗一致；跟随为硬跟随（与战斗相同的像素级锁定，
    /// 平滑阻尼会产生"摄像机跟不上"的滞后感）。缩放按基准偏移等比缩放（高度/距离同比），
    /// 保持俯仰角不随缩放变化。
    /// </summary>
    public class SimulationCameraController : MonoBehaviour
    {
        private Camera _camera;
        private Transform _followTarget;
        private Vector3 _baseOffset = new Vector3(0f, 5f, -3.5f); // 配置基准偏移（zoom=1）
        private Vector3 _followOffset;                            // 当前偏移 = _baseOffset * 缩放系数
        private float _pitchAngle = 60f;
        private float _moveSpeed = 20f;
        private float _zoomSpeed = 5f;
        private float _minZoom = 5f;   // 高度语义（米）
        private float _maxZoom = 30f;
        private float _currentZoom;    // 当前高度
        private bool _isDragging;
        private Vector3 _lastMousePosition;
        private bool _isFollowing = true;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                _camera = gameObject.AddComponent<Camera>();
            }

            // 与战斗相机同源（CameraSystem3D.ApplyConfig）
            var config = Camera3DConfigMgr.Instance;
            _pitchAngle = config.PitchAngle;
            _baseOffset = new Vector3(0f, config.InitialHeight, config.InitialDistance);
            _moveSpeed = config.MoveSpeed;
            _zoomSpeed = config.ZoomSpeed;
            _minZoom = config.MinZoom;
            _maxZoom = config.MaxZoom;
            _currentZoom = config.InitialHeight;
            _followOffset = _baseOffset;

            if (_camera != null && config.Fov > 0f)
            {
                _camera.fieldOfView = config.Fov;
            }
            // 旧版不设旋转，沿用场景里相机残留的俯仰角，与战斗视角不一致；显式钉住
            transform.rotation = Quaternion.Euler(_pitchAngle, 0f, 0f);
        }

        private void Update()
        {
            HandleKeyboardInput();
            HandleMouseInput();
            UpdateCameraPosition();
        }

        private void HandleKeyboardInput()
        {
            // 跟随模式下 WASD 驱动玩家移动，相机不平移
            if (_followTarget != null)
            {
                return;
            }

            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f)
            {
                // 键盘输入时取消跟随，改为手动控制
                if (_isFollowing)
                {
                    _isFollowing = false;
                }

                Vector3 movement = new Vector3(horizontal, 0f, vertical) * _moveSpeed * Time.deltaTime;
                transform.position += movement;
            }
        }

        private void HandleMouseInput()
        {
            // 鼠标悬停在 UI 上时（如 Management 面板滚动列表），滚轮只操作 UI，不缩放视角
            bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            // 鼠标滚轮缩放：以高度为语义，按基准偏移等比缩放，保持俯仰角不变
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (!pointerOverUI && Mathf.Abs(scroll) > 0.01f)
            {
                _currentZoom -= scroll * _zoomSpeed;
                _currentZoom = Mathf.Clamp(_currentZoom, _minZoom, _maxZoom);
                _followOffset = _baseOffset * (_currentZoom / _baseOffset.y);
            }

            // 鼠标右键拖动（跟随模式下禁用，避免相机脱离玩家；从 UI 上起拖时不移动视角）
            if (_followTarget == null && !pointerOverUI && Input.GetMouseButtonDown(1))
            {
                _isDragging = true;
                _lastMousePosition = Input.mousePosition;
                if (_isFollowing)
                {
                    _isFollowing = false;
                }
            }
            else if (Input.GetMouseButtonUp(1))
            {
                _isDragging = false;
            }

            if (_isDragging)
            {
                Vector3 delta = Input.mousePosition - _lastMousePosition;
                Vector3 movement = new Vector3(-delta.x, 0f, -delta.y) * _moveSpeed * Time.deltaTime * 0.1f;
                transform.position += movement;
                _lastMousePosition = Input.mousePosition;
            }
        }

        private void UpdateCameraPosition()
        {
            if (_isFollowing && _followTarget != null)
            {
                // 硬跟随（与战斗相机一致）：相机与玩家像素级锁定，玩家相对屏幕位置恒定，
                // 平滑阻尼会产生"摄像机没跟上"的滞后/顿挫感
                transform.position = _followTarget.position + _followOffset;
            }
            else
            {
                // 手动控制时，确保 Y 坐标为缩放值
                Vector3 position = transform.position;
                position.y = _currentZoom;
                transform.position = position;
            }
        }

        /// <summary>
        /// 设置跟随目标。
        /// </summary>
        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;
            _isFollowing = true;
        }

        /// <summary>
        /// 取消跟随，改为手动控制。
        /// </summary>
        public void StopFollowing()
        {
            _isFollowing = false;
        }

        /// <summary>
        /// 恢复跟随。
        /// </summary>
        public void ResumeFollowing()
        {
            _isFollowing = true;
        }

        /// <summary>
        /// 聚焦到指定位置。
        /// </summary>
        public void FocusOn(Vector3 position)
        {
            _isFollowing = false;
            transform.position = position + _followOffset;
        }
    }
}
