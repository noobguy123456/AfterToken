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
        [SerializeField] private float _wheelTimeScale = 0.2f;

        private Camera _mainCamera;
        private bool _isAimPressed;
        private bool _isWheelOpen;
        private WeaponWheelUI _weaponWheelUI;

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            HandleMoveInput();
            HandleAimInput();
            HandleFireInput();
            HandleAimButtonInput();
            HandleReloadInput();
            HandleWeaponSwitchInput();
            HandleWeaponWheelInput();
            HandleDodgeInput();
        }

        private void HandleMoveInput()
        {
            Vector2 dir = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            ).normalized;

            GameEvent.Get<IBattleInputEvent>().OnMoveInput(dir);
        }

        private void HandleAimInput()
        {
            if (_mainCamera == null) return;

            Vector2 mouseScreenPos = Input.mousePosition;
            Vector2 aimWorldPos = _mainCamera.ScreenToWorldPoint(mouseScreenPos);
            GameEvent.Get<IBattleInputEvent>().OnAimInput(aimWorldPos);
        }

        private void HandleFireInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                GameEvent.Get<IBattleInputEvent>().OnFirePressed();
            }

            if (Input.GetMouseButtonUp(0))
            {
                GameEvent.Get<IBattleInputEvent>().OnFireReleased();
            }
        }

        private void HandleAimButtonInput()
        {
            // 支持 Hold 和 Toggle 两种模式，由 WeaponSystem 处理具体逻辑
            // 这里只发送按下/释放事件
            if (Input.GetKeyDown(_aimKey))
            {
                _isAimPressed = true;
                GameEvent.Get<IBattleInputEvent>().OnAimPressed();
            }

            if (Input.GetKeyUp(_aimKey))
            {
                _isAimPressed = false;
                GameEvent.Get<IBattleInputEvent>().OnAimReleased();
            }
        }

        private void HandleReloadInput()
        {
            if (Input.GetKeyDown(_reloadKey))
            {
                GameEvent.Get<IBattleInputEvent>().OnReloadPressed();
            }
        }

        private void HandleWeaponSwitchInput()
        {
            if (_isWheelOpen) return;

            float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
            if (scroll > 0)
            {
                GameEvent.Get<IBattleInputEvent>().OnWeaponSwitch(1);
            }
            else if (scroll < 0)
            {
                GameEvent.Get<IBattleInputEvent>().OnWeaponSwitch(-1);
            }
        }

        private void HandleWeaponWheelInput()
        {
            if (Input.GetKeyDown(_weaponWheelKey))
            {
                _isWheelOpen = true;
                Time.timeScale = _wheelTimeScale;
                GameModule.UI.ShowUIAsync<WeaponWheelUI>();
                GameEvent.Get<IBattleInputEvent>().OnWeaponWheelToggled(true);
            }

            if (Input.GetKeyUp(_weaponWheelKey))
            {
                _isWheelOpen = false;
                Time.timeScale = 1f;

                int selectedSlot = 0;
                if (_weaponWheelUI != null)
                {
                    selectedSlot = _weaponWheelUI.GetSelectedSlot();
                }
                else
                {
                    selectedSlot = CalculateWheelSlot();
                }

                GameEvent.Get<IBattleInputEvent>().OnWeaponSelected(selectedSlot);
                GameEvent.Get<IBattleInputEvent>().OnWeaponWheelToggled(false);
                GameModule.UI.CloseUI<WeaponWheelUI>();
                _weaponWheelUI = null;
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
                GameEvent.Get<IBattleInputEvent>().OnDodgePressed();
            }
        }

        public bool IsAimPressed => _isAimPressed;
        public bool IsWheelOpen => _isWheelOpen;
    }
}
