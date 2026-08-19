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
        [SerializeField] private KeyCode _bagKey = KeyCode.B;
        [SerializeField] private float _wheelTimeScale = 0.2f;

        private Camera _mainCamera;
        private bool _isAimPressed;
        private bool _isWheelOpen;
        private bool _menuUIOpenLast;
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
            _mainCamera = CameraSystem3D.Instance?.GetMainCamera();
        }

        private void Update()
        {
            // 暂停 UI 打开时，Time.timeScale 可能为 0。
            // ESC 作为全局关闭键，始终响应；其他战斗输入在暂停时跳过。
            HandleEscapeInput();

            if (Time.timeScale <= Mathf.Epsilon)
            {
                return;
            }

            HandleMoveInput();

            // 菜单类 UI（背包/开箱/纸条）打开时屏蔽鼠标战斗输入，防止点 UI 时开枪；
            // 打开瞬间补发释放事件，避免按住开火/瞄准键开 UI 后武器卡在按下状态
            bool menuUIOpen = IsMenuUIOpen();
            if (menuUIOpen && !_menuUIOpenLast)
            {
                BattleInputEvent?.OnFireReleased();
                if (_isAimPressed)
                {
                    _isAimPressed = false;
                    BattleInputEvent?.OnAimReleased();
                }
            }
            _menuUIOpenLast = menuUIOpen;

            if (!menuUIOpen)
            {
                // 瞄准射线也要屏蔽：菜单打开时准星已冻结（CrosshairUpdater），
                // 继续发瞄准事件会让角色/武器朝向跟着一个不动的点之外的状态走
                HandleAimInput();
                HandleFireInput();
                HandleAimButtonInput();
            }

            HandleReloadInput();
            HandleWeaponSwitchInput();
            HandleWeaponWheelInput();
            HandleDodgeInput();
            HandleCrosshairStyleInput();
            HandleInteractInput();
            HandleBagInput();
        }

        private void HandleMoveInput()
        {
            Vector2 dir = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );

            if (dir.sqrMagnitude > 0f)
            {
                // 相机系相对移动：WASD 始终对齐屏幕方向（相机偏航旋转后 W 仍是屏幕上方），
                // 无相机时退回世界系（ yaw=0 时两者一致）
                dir = ToCameraSpace(dir).normalized;
            }
            else
            {
                dir = Vector2.zero;
            }

            BattleInputEvent?.OnMoveInput(dir);
        }

        /// <summary>
        /// 将屏幕系输入（x=右, y=上）映射到玩法平面 XZ 上的相机相对方向。
        /// </summary>
        private Vector2 ToCameraSpace(Vector2 dir)
        {
            // Start 时相机可能尚未就绪，这里懒获取兜底
            if (_mainCamera == null)
            {
                _mainCamera = CameraSystem3D.Instance?.GetMainCamera();
            }
            if (_mainCamera == null) return dir;

            Vector3 forward = _mainCamera.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-6f) return dir;
            forward.Normalize();

            // 俯仰角不影响方向映射，只取偏航分量
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 world = right * dir.x + forward * dir.y;
            return new Vector2(world.x, world.z);
        }

        private void HandleAimInput()
        {
            if (_mainCamera == null) return;

            // 使用 CrosshairUpdater 的屏幕位置作为瞄准点，确保射击方向和准星一致。
            // 当系统光标被锁定时，Input.mousePosition 会固定在屏幕中心，不能直接使用。
            Vector2 aimScreenPos = CrosshairUpdater.Instance != null
                ? CrosshairUpdater.Instance.CurrentScreenPos
                : (Vector2)Input.mousePosition;

            // 透视相机下 ScreenToWorldPoint 必须指定深度，直接传 Vector2（z=0）会得到相机自身位置。
            // 统一用“相机射线 × 玩法平面（y=0 的 XZ 地面）”求交，正交/透视相机都适用。
            // 开镜狙击也是主相机射线：狙击镜为 Duckov 式放大镜，活动范围=主相机渲染区域。
            Ray ray = _mainCamera.ScreenPointToRay(aimScreenPos);
            var gameplayPlane = new Plane(Vector3.up, Vector3.zero);
            if (!gameplayPlane.Raycast(ray, out float enter))
            {
                // 射线与玩法平面平行（理论上不会发生），保持上一次瞄准位置。
                return;
            }

            Vector3 hitPoint = ray.GetPoint(enter);
            BattleInputEvent?.OnAimInput(new Vector2(hitPoint.x, hitPoint.z));
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
            // 战斗中系统光标被锁定在屏幕中心，Input.mousePosition 恒为中心点，
            // 必须使用 CrosshairUpdater 的虚拟准星屏幕位置，否则永远选中 slot 0。
            Vector2 mousePos = CrosshairUpdater.Instance != null
                ? CrosshairUpdater.Instance.CurrentScreenPos
                : (Vector2)Input.mousePosition;
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

        /// <summary>
        /// ESC 键全局处理：优先关闭当前最上层可关闭 UI；没有任何 UI 打开时打开设置面板。
        /// 与 UI 内部关闭按钮不冲突（关闭按钮直接调用 CloseUI）。
        /// </summary>
        private void HandleEscapeInput()
        {
            if (!Input.GetKeyDown(_settingsKey))
            {
                return;
            }

            // 按 UI 层级从高到低尝试关闭最上层弹窗；一次 ESC 只关闭一个。
            // 顺序：SettingsUI > BattleBagUI > LootContainerUI > NoteUI（后续可扩展 WeaponWheelUI 等）
            if (TryCloseUI<SettingsUI>()) return;
            if (TryCloseUI<BattleBagUI>()) return;
            if (TryCloseUI<LootContainerUI>()) return;
            if (TryCloseUI<NoteUI>()) return;

            // 没有可关闭 UI 时打开设置面板
            GameModule.UI.ShowUIAsync<SettingsUI>();
        }

        private bool TryCloseUI<T>() where T : UIWindow, new()
        {
            if (GameModule.UI.HasWindow<T>())
            {
                GameModule.UI.CloseUI<T>();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 是否有菜单类 UI 打开（背包/开箱/纸条）。打开期间屏蔽射击与瞄准输入，
        /// 并冻结准星与角色朝向（PlayerEntity/CrosshairUpdater 也读取此状态）。
        /// </summary>
        public static bool IsMenuUIOpen()
        {
            return GameModule.UI.HasWindow<BattleBagUI>()
                || GameModule.UI.HasWindow<LootContainerUI>()
                || GameModule.UI.HasWindow<NoteUI>();
        }

        private void HandleBagInput()
        {
            if (Input.GetKeyDown(_bagKey))
            {
                if (GameModule.UI.HasWindow<BattleBagUI>())
                {
                    GameModule.UI.CloseUI<BattleBagUI>();
                }
                else
                {
                    GameModule.UI.ShowUIAsync<BattleBagUI>();
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
