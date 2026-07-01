using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 准星更新器。
    /// 作为 MonoBehaviour 挂载在准星节点上，确保即使 BattleMainUI 被 UI 栈隐藏时，
    /// 准星位置仍能被刷新并跟随鼠标指针。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CrosshairUpdater : MonoBehaviour
    {
        public static CrosshairUpdater Instance { get; private set; }

        [SerializeField] private RectTransform _crosshair;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private BattleMainUI _owner;

        private Vector2 _currentScreenPos;
        public Vector2 CurrentScreenPos => _currentScreenPos;

        private void Awake()
        {
            Instance = this;
            if (_crosshair == null) _crosshair = transform as RectTransform;
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();

            GameEvent.AddEventListener(IBattleInputEvent_Event.OnCycleCrosshairStyle, OnCycleCrosshairStyle);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            GameEvent.RemoveEventListener(IBattleInputEvent_Event.OnCycleCrosshairStyle, OnCycleCrosshairStyle);
        }

        private void Start()
        {
            // 战斗流程会锁定系统光标，Input.mousePosition 会被固定在屏幕中心，
            // 因此用鼠标位移累加来驱动准星。
            _currentScreenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        public void Initialize(BattleMainUI owner, RectTransform crosshair, Canvas canvas)
        {
            _owner = owner;
            _crosshair = crosshair;
            _canvas = canvas;
        }

        private void Update()
        {
            // 游戏暂停（Time.timeScale = 0）时，鼠标位移不应再驱动准星，
            // 避免在设置面板等 UI 上移动鼠标时场景准星跟随。
            if (Time.timeScale <= Mathf.Epsilon)
            {
                return;
            }

            UpdatePosition();
            UpdateRotation();
        }

        /// <summary>
        /// 更新准星位置。
        /// 使用鼠标位移累加，避免 Cursor.lockState=Locked 时 Input.mousePosition 被固定。
        /// </summary>
        private void UpdatePosition()
        {
            if (_crosshair == null) return;

            var parent = _crosshair.parent as RectTransform;
            if (parent == null) return;

            _currentScreenPos.x += Input.GetAxis("Mouse X") * SensitivitySetting.Value;
            _currentScreenPos.y += Input.GetAxis("Mouse Y") * SensitivitySetting.Value;
            _currentScreenPos.x = Mathf.Clamp(_currentScreenPos.x, 0f, Screen.width);
            _currentScreenPos.y = Mathf.Clamp(_currentScreenPos.y, 0f, Screen.height);

            Camera cam = _canvas != null ? _canvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, _currentScreenPos, cam, out Vector2 localPos))
            {
                _crosshair.anchoredPosition = localPos;
            }
        }

        /// <summary>
        /// 换弹期间让转圈准星持续旋转。
        /// </summary>
        private void UpdateRotation()
        {
            if (_crosshair == null || _owner == null) return;

            if (_owner.IsReloading)
            {
                _crosshair.Rotate(0f, 0f, -_owner.ReloadingSpinSpeed * Time.deltaTime);
            }
            else
            {
                _crosshair.localRotation = Quaternion.identity;
            }
        }

        private void OnCycleCrosshairStyle()
        {
            _owner?.CycleCrosshairStyle();
        }
    }
}
