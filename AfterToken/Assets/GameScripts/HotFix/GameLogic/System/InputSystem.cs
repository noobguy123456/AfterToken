using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 输入系统。
    /// 负责读取玩家输入并转换为事件。
    /// </summary>
    public class InputSystem : MonoBehaviour
    {
        [Header("输入设置")]
        [SerializeField] private KeyCode _aimKey = KeyCode.Mouse1;
        [SerializeField] private KeyCode _weaponWheelKey = KeyCode.Tab;
        [SerializeField] private KeyCode _reloadKey = KeyCode.R;
        [SerializeField] private KeyCode _dodgeKey = KeyCode.Space;
        [SerializeField] private KeyCode _interactKey = KeyCode.E;
        [SerializeField] private KeyCode _settingsKey = KeyCode.Escape;
        [SerializeField] private float _wheelTimeScale = 0.2f;

        private Camera _mainCamera;
        private bool _isAimPressed;
        private bool _isWheelOpen;
        private WeaponWheelUI _weaponWheelUI;
        private IBattleInputEvent _battleInputEvent;
        private CancellationTokenSource _weaponWheelCts;

        private IBattleInputEvent BattleInputEvent
        {
            get
            {
                if (_battleInputEvent == null)
                {
                    _battleInputEvent = GameEvent.Get<IBattleInputEvent>();
                }

                return _battleInputEvent;
            }
        }

        private void Start()
        {
            _mainCamera = CameraSystem.Instance?.MainCamera;
        }

        private void Update()
        {
            // 设置面板等暂停 UI 打开时，Time.timeScale 为 0。
            // 此时只处理全局输入（如关闭设置面板），避免战斗输入穿透到游戏场景导致相机/角色抖动。
            HandleSettingsInput();

            if (Time.timeScale <= Mathf.Epsilon)
            {
                return;
            }

            HandleMoveInput();
            HandleAimInput();
            HandleFireInput();
            HandleAimButtonInput();
            HandleReloadInput();
            HandleWeaponSwitchInput();
            HandleWeaponWheelInput();
            HandleDodgeInput();
            HandleCrosshairStyleInput();
            HandleInteractInput();
        }

        private void HandleMoveInput()
        {
            Vector2 dir = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            ).normalized;

            BattleInputEvent?.OnMoveInput(dir);
        }

        private void HandleAimInput()
        {
            if (_mainCamera == null) return;

            // 使用 CrosshairUpdater 的屏幕位置作为瞄准点，确保射击方向和准星一致。
            // 当系统光标被锁定时，Input.mousePosition 会固定在屏幕中心，不能直接使用。
            Vector2 aimScreenPos = CrosshairUpdater.Instance != null
                ? CrosshairUpdater.Instance.CurrentScreenPos
                : (Vector2)Input.mousePosition;

            Vector2 aimWorldPos = _mainCamera.ScreenToWorldPoint(aimScreenPos);
            BattleInputEvent?.OnAimInput(aimWorldPos);
        }

        private void HandleFireInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                BattleInputEvent?.OnFirePressed();
            }

            if (Input.GetMouseButtonUp(0))
            {
                BattleInputEvent?.OnFireReleased();
            }
        }

        private void HandleAimButtonInput()
        {
            // 支持 Hold 和 Toggle 两种模式，由 WeaponSystem 处理具体逻辑
            // 这里只发送按下/释放事件
            if (Input.GetKeyDown(_aimKey))
            {
                _isAimPressed = true;
                BattleInputEvent?.OnAimPressed();
            }

            if (Input.GetKeyUp(_aimKey))
            {
                _isAimPressed = false;
                BattleInputEvent?.OnAimReleased();
            }
        }

        private void HandleReloadInput()
        {
            if (Input.GetKeyDown(_reloadKey))
            {
                BattleInputEvent?.OnReloadPressed();
            }
        }

        private void HandleWeaponSwitchInput()
        {
            if (_isWheelOpen) return;

            float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
            if (scroll > 0)
            {
                BattleInputEvent?.OnWeaponSwitch(1);
            }
            else if (scroll < 0)
            {
                BattleInputEvent?.OnWeaponSwitch(-1);
            }
        }

        private void HandleWeaponWheelInput()
        {
            if (Input.GetKeyDown(_weaponWheelKey))
            {
                _isWheelOpen = true;
                // 武器轮盘属于输入层触发的全局时间缩放效果，通过 GamePauseManager 统一控制。
                GamePauseManager.PushTimeScale(_wheelTimeScale);
                ShowWeaponWheelAsync().Forget();;
                BattleInputEvent?.OnWeaponWheelToggled(true);
            }

            if (Input.GetKeyUp(_weaponWheelKey))
            {
                _isWheelOpen = false;
                GamePauseManager.PopTimeScale();

                int selectedSlot = _weaponWheelUI != null
                    ? _weaponWheelUI.GetSelectedSlot()
                    : CalculateWheelSlot();

                BattleInputEvent?.OnWeaponSelected(selectedSlot);
                BattleInputEvent?.OnWeaponWheelToggled(false);
                GameModule.UI.CloseUI<WeaponWheelUI>();
                _weaponWheelUI = null;
            }
        }

        private async UniTaskVoid ShowWeaponWheelAsync()
        {
            _weaponWheelCts?.Cancel();
            _weaponWheelCts?.Dispose();
            _weaponWheelCts = new CancellationTokenSource();

            try
            {
                _weaponWheelUI = await GameModule.UI.ShowUIAsyncAwait<WeaponWheelUI>(_weaponWheelCts.Token);
            }
            catch (OperationCanceledException)
            {
                // 输入系统销毁时取消，忽略异常。
            }
        }

        private int CalculateWheelSlot()
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 dir = (mousePos - center).normalized;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            angle = (angle + 360f) % 360f;

            if (angle < 120f) return 0;
            if (angle < 240f) return 1;
            return 2;
        }

        private void HandleDodgeInput()
        {
            if (Input.GetKeyDown(_dodgeKey))
            {
                BattleInputEvent?.OnDodgePressed();
            }
        }

        private void HandleSettingsInput()
        {
            if (Input.GetKeyDown(_settingsKey))
            {
                if (GameModule.UI.HasWindow<SettingsUI>())
                {
                    GameModule.UI.CloseUI<SettingsUI>();
                }
                else
                {
                    GameModule.UI.ShowUIAsync<SettingsUI>();
                }
            }
        }

        private void OnDestroy()
        {
            _weaponWheelCts?.Cancel();
            _weaponWheelCts?.Dispose();
            _weaponWheelCts = null;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) return;

            // 切出窗口时清空可能卡住的输入状态，避免返回后按键/鼠标状态异常。
            if (_isWheelOpen)
            {
                _isWheelOpen = false;
                GamePauseManager.PopTimeScale();
            }
            _isAimPressed = false;
            _battleInputEvent?.OnAimReleased();
            _battleInputEvent?.OnFireReleased();
            _battleInputEvent?.OnWeaponWheelToggled(false);
        }

        private void HandleCrosshairStyleInput()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                BattleInputEvent?.OnCycleCrosshairStyle();
            }
        }

        private void HandleInteractInput()
        {
            if (Input.GetKeyDown(_interactKey))
            {
                BattleInputEvent?.OnInteractPressed();
            }
        }

        public bool IsAimPressed => _isAimPressed;
        public bool IsWheelOpen => _isWheelOpen;
    }
}
