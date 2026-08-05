using TEngine;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameLogic
{
    /// <summary>
    /// 经营场景相机控制器：支持跟随目标、WASD 移动、鼠标拖动、滚轮缩放。
    /// </summary>
    public class SimulationCameraController : MonoBehaviour
    {
        private Camera _camera;
        private Transform _followTarget;
        private Vector3 _followOffset = new Vector3(0f, 7f, -5f);
        private float _moveSpeed = 50f; // 增加移动速度，从 20 改为 50
        private float _zoomSpeed = 5f;
        private float _minZoom = 4f;
        private float _maxZoom = 20f;
        private float _currentZoom = 7f;
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
            _currentZoom = _followOffset.y;
            Log.Info($"[SimulationCameraController] 初始化完成，位置: {transform.position}, 缩放: {_currentZoom}");
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
                    Log.Info("[SimulationCameraController] 键盘输入，取消跟随，改为手动控制");
                }
                
                Vector3 movement = new Vector3(horizontal, 0f, vertical) * _moveSpeed * Time.deltaTime;
                transform.position += movement;
            }
        }

        private void HandleMouseInput()
        {
            // 鼠标悬停在 UI 上时（如 Management 面板滚动列表），滚轮只操作 UI，不缩放视角
            bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            // 鼠标滚轮缩放
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (!pointerOverUI && Mathf.Abs(scroll) > 0.01f)
            {
                _currentZoom -= scroll * _zoomSpeed;
                _currentZoom = Mathf.Clamp(_currentZoom, _minZoom, _maxZoom);
                _followOffset.y = _currentZoom;
            }

            // 鼠标右键拖动（跟随模式下禁用，避免相机脱离玩家；从 UI 上起拖时不移动视角）
            if (_followTarget == null && !pointerOverUI && Input.GetMouseButtonDown(1))
            {
                _isDragging = true;
                _lastMousePosition = Input.mousePosition;
                if (_isFollowing)
                {
                    _isFollowing = false;
                    Log.Info("[SimulationCameraController] 鼠标拖动，取消跟随，改为手动控制");
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
                // 跟随目标：指数阻尼，系数与帧率无关
                Vector3 targetPos = _followTarget.position + _followOffset;
                float t = 1f - Mathf.Exp(-Time.deltaTime * 5f);
                transform.position = Vector3.Lerp(transform.position, targetPos, t);
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
            Log.Info($"[SimulationCameraController] 设置跟随目标: {target?.name ?? "null"}");
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
            transform.position = new Vector3(position.x, _currentZoom, position.z - 5f);
        }
    }
}
