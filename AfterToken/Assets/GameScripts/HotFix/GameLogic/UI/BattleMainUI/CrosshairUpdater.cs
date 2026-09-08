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

        /// <summary>
        /// 设置准星节点是否可见。
        /// 打开背包/设置等 UI 时应隐藏准星（隐藏即冻结，位置不会被任何 UI 操作改动）。
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_crosshair != null)
            {
                _crosshair.gameObject.SetActive(visible);
            }
        }

        private void Update()
        {
            // 核心原则：准星位置是玩家的瞄准状态，只有战斗中的鼠标位移能驱动它。
            // 任何 UI 打开期间一律冻结（不强制移动、不同步系统鼠标），关掉 UI 后瞄点原样保留。
            bool cursorVisible = CursorManager.Instance != null && CursorManager.Instance.IsCursorVisible;
            if (cursorVisible)
            {
                // 兜底：没有任何菜单 UI 光标却可见（ShowCursor/HideCursor 未严格配对导致
                // 引用计数泄漏，表现为关掉 UI 后 Windows 系统鼠标仍显示），强制恢复战斗光标状态。
                bool menuOpen = InputSystem.IsMenuUIOpen()
                    || CompanionChatUI.IsOpen
                    || GameModule.UI.HasWindow<WeaponWheelUI>()
                    || GameModule.UI.HasWindow<SettingsUI>();
                if (!menuOpen && Time.timeScale > Mathf.Epsilon)
                {
                    CursorManager.Instance.ForceHideCursor();
                }
                return;
            }

            // 武器轮盘期间光标保持锁定隐藏（轮盘用自己的增量选择，不经系统鼠标），准星同样冻结。
            // 正常路径下轮盘会把准星 SetVisible(false)（本组件随之停走），这里是兜底。
            if (GameModule.UI.HasWindow<WeaponWheelUI>())
            {
                return;
            }

            // 游戏暂停（Time.timeScale = 0）时，鼠标位移不应再驱动准星。
            if (Time.timeScale <= Mathf.Epsilon)
            {
                return;
            }

            // 战斗状态下每帧断言系统光标隐藏+锁定：Windows/编辑器下 Cursor.visible=false
            // 偶发不真正生效（CursorManager 注释记录的已知问题），引用计数归零后 OS 鼠标
            // 可能仍残留在屏幕上。这里无条件重设，彻底杜绝战斗中出现系统鼠标。
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

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

            // 开镜（狙击镜）时使用独立的开镜灵敏度，与不开镜灵敏度互不影响
            float sensitivity = WeaponSystem.Instance != null && WeaponSystem.Instance.IsScopedSniping
                ? SensitivitySetting.ScopedValue
                : SensitivitySetting.Value;

            _currentScreenPos.x += Input.GetAxis("Mouse X") * sensitivity;
            _currentScreenPos.y += Input.GetAxis("Mouse Y") * sensitivity;
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
