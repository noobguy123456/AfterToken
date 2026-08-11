using System.Collections.Generic;
using GameLogic.Portal;
using UnityEngine;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 武器系统。
    /// 负责管理武器槽、切换、瞄准、开火。
    /// </summary>
    public class WeaponSystem : MonoBehaviour
    {
        public static WeaponSystem Instance { get; private set; }

        public const int MAX_WEAPON_SLOTS = 3;

        [Header("瞄准设置")]
        [SerializeField] private AimMode _aimMode = AimMode.Hold;

        public WeaponInstance[] Slots { get; private set; }
        private int _currentSlot = 0;
        private IWeaponOwner _owner;
        private int[] _defaultWeaponIds;
        // 瞄准状态的唯一数据源是玩家黑板 PlayerStateContext.IsAiming（通过 _owner 转发访问）；
        // 本字段仅是黑板未就绪（SetOwner 之前）时的本地兜底，行为与原私有字段一致。
        private bool _isAimingFallback;
        private bool _isFiring;
        private bool _firePending;
        private float _lastSwitchTime;

        private readonly GameEventMgr _eventMgr = new GameEventMgr();

        private void Awake()
        {
            Instance = this;
            Slots = new WeaponInstance[MAX_WEAPON_SLOTS];

            _eventMgr.AddEvent<Vector2>(IBattleInputEvent_Event.OnMoveInput, OnMoveInput);
            _eventMgr.AddEvent(IBattleInputEvent_Event.OnFirePressed, OnFirePressed);
            _eventMgr.AddEvent(IBattleInputEvent_Event.OnFireReleased, OnFireReleased);
            _eventMgr.AddEvent(IBattleInputEvent_Event.OnAimPressed, OnAimPressed);
            _eventMgr.AddEvent(IBattleInputEvent_Event.OnAimReleased, OnAimReleased);
            _eventMgr.AddEvent<int>(IWeaponEvent_Event.OnReload, OnReload);
            _eventMgr.AddEvent<int>(IBattleInputEvent_Event.OnWeaponSwitch, OnWeaponSwitch);
            _eventMgr.AddEvent<int>(IBattleInputEvent_Event.OnWeaponSelected, OnWeaponSelected);
        }

        private void OnDestroy()
        {
            _eventMgr.Clear();
            Instance = null;

            for (int i = 0; i < MAX_WEAPON_SLOTS; i++)
            {
                Slots[i]?.Dispose();
                Slots[i] = null;
            }
        }

        private void Start()
        {
            if (PortalPlayerState.HasSavedState && PortalPlayerState.Weapons != null)
            {
                for (int i = 0; i < MAX_WEAPON_SLOTS && i < PortalPlayerState.Weapons.Length; i++)
                {
                    var data = PortalPlayerState.Weapons[i];
                    if (data.IsValid)
                    {
                        EquipWeapon(i, data.ConfigId);
                        GetWeaponInSlot(i)?.SetAmmo(data.CurrentAmmo);
                    }
                }
                SwitchToSlot(PortalPlayerState.CurrentWeaponSlot);
            }
            else
            {
                // 从关卡配置或默认档案读取当前携带的武器
                var defaults = _defaultWeaponIds ?? new[] { 1001, 1002, 1003 };
                for (int i = 0; i < defaults.Length && i < MAX_WEAPON_SLOTS; i++)
                {
                    EquipWeapon(i, defaults[i]);
                }

                SwitchToSlot(0);
            }
        }

        /// <summary>
        /// 设置武器所有者。由 PlayerSystem 在创建玩家后注入，解除 WeaponSystem 对 PlayerSystem 的直接依赖。
        /// </summary>
        public void SetOwner(IWeaponOwner owner)
        {
            _owner = owner;
        }

        private void Update()
        {
            var weapon = CurrentWeapon;
            if (weapon != null)
            {
                weapon.Tick(Time.deltaTime, _owner?.IsMoving ?? false, IsAiming);
            }

            if (_firePending || (_isFiring && weapon != null && weapon.Config.fireMode == FireMode.Auto))
            {
                TryFire();
            }
        }

        public WeaponInstance CurrentWeapon => Slots[_currentSlot];
        public int CurrentSlotIndex => _currentSlot;

        /// <summary>
        /// 玩家黑板（瞄准状态的宿主）。_owner 由 PlayerSystem 在创建黑板之后注入，故非空时黑板必然存在。
        /// </summary>
        private PlayerStateContext AimContext => (_owner as PlayerEntity)?.Context;

        /// <summary>
        /// 是否正在瞄准（转发自玩家黑板 <see cref="PlayerStateContext.IsAiming"/>，黑板未就绪时读本地兜底）。
        /// </summary>
        public bool IsAiming => AimContext?.IsAiming ?? _isAimingFallback;
        public bool IsFiring => _isFiring;
        public AimMode CurrentAimMode => _aimMode;

        /// <summary>
        /// 获取指定武器槽位中的武器实例。
        /// </summary>
        public WeaponInstance GetWeaponInSlot(int slot)
        {
            if (slot < 0 || slot >= MAX_WEAPON_SLOTS) return null;
            return Slots[slot];
        }

        /// <summary>
        /// 设置默认武器（需在 Start 前调用）。
        /// </summary>
        public void SetDefaultWeapons(int[] weaponIds)
        {
            _defaultWeaponIds = weaponIds;
        }

        /// <summary>
        /// 装备武器到指定槽位。
        /// </summary>
        public void EquipWeapon(int slot, int weaponConfigId)
        {
            if (slot < 0 || slot >= MAX_WEAPON_SLOTS) return;

            var config = WeaponConfigMgr.Instance?.Get(weaponConfigId);
            if (config == null)
            {
                Log.Warning($"[WeaponSystem] 找不到武器配置 {weaponConfigId}");
                return;
            }

            Slots[slot]?.Dispose();
            Slots[slot] = new WeaponInstance(config);
            GameEvent.Get<IWeaponEvent>().OnWeaponEquipped(_owner?.OwnerId ?? 0, slot, weaponConfigId);
        }

        /// <summary>
        /// 滚轮切换武器。
        /// </summary>
        private void OnWeaponSwitch(int delta)
        {
            if (!CanSwitch()) return;

            int newSlot = _currentSlot + delta;
            if (newSlot < 0) newSlot = MAX_WEAPON_SLOTS - 1;
            if (newSlot >= MAX_WEAPON_SLOTS) newSlot = 0;

            SwitchToSlot(newSlot);
        }

        /// <summary>
        /// 轮盘/数字键切换武器。
        /// </summary>
        private void OnWeaponSelected(int slot)
        {
            if (!CanSwitch()) return;
            if (slot < 0 || slot >= MAX_WEAPON_SLOTS) return;
            if (slot == _currentSlot) return;

            SwitchToSlot(slot);
        }

        public void SwitchToSlot(int slot)
        {
            var prevWeapon = CurrentWeapon;
            if (prevWeapon != null && prevWeapon.IsReloading)
            {
                prevWeapon.CancelReload(_owner?.OwnerId ?? 0);
            }

            // 切换武器时取消瞄准——必须在换槽之前执行：
            // SetAimState 内部按 CurrentWeapon 判断是否狙击枪来决定关狙击镜，
            // 换槽后旧狙击镜会漏关（狙击镜 UI/相机卡在开镜状态）
            if (IsAiming)
            {
                SetAimState(false);
            }

            _currentSlot = slot;
            _lastSwitchTime = Time.time;
            _isFiring = false;
            _firePending = false;

            GameEvent.Get<IWeaponEvent>().OnWeaponSwitched(_owner?.OwnerId ?? 0, slot);
            GameEvent.Get<IPlayerEvent>().OnAmmoChanged(
                CurrentWeapon?.CurrentAmmo ?? 0,
                CurrentWeapon?.Config.clipSize ?? 0);
        }

        private bool CanSwitch()
        {
            return Time.time - _lastSwitchTime >= PlayerConfigWeaponSwitchCooldown;
        }

        /// <summary>
        /// 从玩家配置读取武器切换冷却（兜底 0.3s）。
        /// </summary>
        private static float PlayerConfigWeaponSwitchCooldown
        {
            get
            {
                try
                {
                    var cfg = ConfigSystem.Instance?.Tables?.TbPlayer?.GetOrDefault(1);
                    if (cfg != null && cfg.WeaponSwitchCooldown > 0)
                    {
                        return cfg.WeaponSwitchCooldown;
                    }
                }
                catch
                {
                    // ignored
                }
                return 0.3f;
            }
        }

        private void OnFirePressed()
        {
            var weapon = CurrentWeapon;
            if (weapon == null) return;

            if (weapon.Config.fireMode == FireMode.Auto)
            {
                _isFiring = true;
                GameEvent.Get<IWeaponEvent>().OnStartFire(_owner?.OwnerId ?? 0);
            }
            else
            {
                _firePending = true;
            }
        }

        private void OnFireReleased()
        {
            _isFiring = false;
            GameEvent.Get<IWeaponEvent>().OnStopFire(_owner?.OwnerId ?? 0);
        }

        private void OnAimPressed()
        {
            if (GetEffectiveAimMode() == AimMode.Hold)
            {
                SetAimState(true);
            }
            else
            {
                SetAimState(!IsAiming);
            }
        }

        private void OnAimReleased()
        {
            if (GetEffectiveAimMode() == AimMode.Hold)
            {
                SetAimState(false);
            }
        }

        /// <summary>
        /// 狙击枪的开镜模式走设置面板（长按/切换，见 SniperAimModeSetting），其他武器用序列化的默认模式。
        /// </summary>
        private AimMode GetEffectiveAimMode()
        {
            if (CurrentWeapon?.Config.weaponType == WeaponType.Sniper)
            {
                return SniperAimModeSetting.IsToggle ? AimMode.Toggle : AimMode.Hold;
            }
            return _aimMode;
        }

        /// <summary>
        /// 当前是否处于狙击开镜状态（弹道系统据此跳过 tracer 视觉，实现“直接命中”观感）。
        /// 开镜即有镜窗图案（纯视觉），是否放大由 scopeFov 决定（0=无放大，见 SetAimState）。
        /// </summary>
        public bool IsScopedSniping => IsAiming
            && CurrentWeapon != null
            && CurrentWeapon.Config.weaponType == WeaponType.Sniper;

        private void SetAimState(bool aiming)
        {
            if (IsAiming == aiming) return;

            // 写入唯一数据源：玩家黑板；黑板未就绪时写本地兜底（与原私有字段行为一致）
            var context = AimContext;
            if (context != null)
            {
                context.IsAiming = aiming;
            }
            else
            {
                _isAimingFallback = aiming;
            }

            GameEvent.Get<IWeaponEvent>().OnAimStateChanged(_owner?.OwnerId ?? 0, aiming);

            // 更新相机 FOV（狙击枪走 Duckov 式狙击镜，主相机不变焦）
            bool isSniper = CurrentWeapon?.Config.weaponType == WeaponType.Sniper;
            if (!isSniper)
            {
                float defaultFov = 60f;
                try
                {
                    defaultFov = ConfigSystem.Instance?.Tables?.TbCamera?.GetOrDefault(1)?.DefaultFov ?? 60f;
                }
                catch
                {
                    // ignored
                }
                float targetFov = IsAiming && CurrentWeapon != null
                    ? CurrentWeapon.Config.aimFov
                    : defaultFov;
                GameEvent.Get<ICameraEvent>().OnAimFovChanged(targetFov);
            }

            // 狙击枪开镜时打开瞄准镜 UI（纯视觉镜窗）；scopeFov>0 时才启用镜相机放大，
            // scopeFov=0 表示无放大——镜内外画面一致，仅灰色蒙版 + 镜窗图案
            if (isSniper)
            {
                if (IsAiming)
                {
                    if (CurrentWeapon.ScopeFov > 0f)
                    {
                        CameraSystem3D.Instance?.SetScopeActive(true, CurrentWeapon.ScopeFov);
                    }
                    GameModule.UI.ShowUIAsync<SniperScopeUI>();
                }
                else
                {
                    CameraSystem3D.Instance?.SetScopeActive(false);
                    GameModule.UI.CloseUI<SniperScopeUI>();
                }
            }
        }

        private void OnReload(int ownerId)
        {
            // 仅处理当前所有者的换弹请求
            if (_owner != null && ownerId != _owner.OwnerId) return;
            CurrentWeapon?.Reload(_owner?.OwnerId ?? 0);
        }

        private void OnMoveInput(Vector2 direction)
        {
            // 移动状态由 PlayerSystem 处理，WeaponSystem 只提供移速系数
        }

        private void TryFire()
        {
            var weapon = CurrentWeapon;
            if (weapon == null) return;

            // 弹匣为空时按开火键自动换弹。
            // Reload 内部已去重（换弹中/满弹匣直接返回），连发模式按住不放不会重复触发；
            // 单发模式的本次开火意图在此消费，避免换弹完成后意外击发一发。
            if (weapon.CurrentAmmo <= 0)
            {
                if (weapon.Config.fireMode != FireMode.Auto)
                {
                    _firePending = false;
                }
                weapon.Reload(_owner?.OwnerId ?? 0);
                return;
            }

            if (!weapon.CanFire(Time.time)) return;

            if (_owner == null) return;

            Vector2 origin = _owner.Position;
            Vector2 aimPos = _owner.AimPosition;
            Vector2 rawDirection = (aimPos - origin).normalized;
            Vector2 direction;

            if (IsScopedSniping)
            {
                // 开镜狙击：直接命中镜窗中心（= AimPosition），跳过辅助瞄准与扩散
                direction = rawDirection;
            }
            else
            {
                // 辅助瞄准修正
                direction = AimAssistSystem.Instance?.ApplyAimAssist(
                    origin,
                    rawDirection,
                    weapon.Config.id,
                    IsAiming) ?? rawDirection;

                // 扩散
                float spread = weapon.CalculateSpread(
                    _owner.IsMoving,
                    IsAiming);
                direction = ApplySpread(direction, spread);
            }

            weapon.Fire(origin, direction, _owner.OwnerId);

            if (weapon.Config.fireMode != FireMode.Auto)
            {
                _firePending = false;
            }

            // 射击后相机抖动：根据武器后坐力强度计算，未配置时给予基础抖动。
            float recoil = weapon.Config.recoilIntensity > 0f ? weapon.Config.recoilIntensity : 2f;
            float shakeMag = recoil * 0.25f;
            GameEvent.Get<ICameraEvent>()?.OnCameraShake(shakeMag, 0.1f);
        }

        private Vector2 ApplySpread(Vector2 direction, float spread)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            angle += Random.Range(-spread * 0.5f, spread * 0.5f);
            float rad = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        /// <summary>
        /// 获取当前武器的移动速度系数。
        /// </summary>
        public float GetCurrentMoveSpeedMultiplier()
        {
            if (CurrentWeapon == null) return 1f;

            float multiplier = CurrentWeapon.Config.moveSpeedMultiplier;
            if (_isFiring) multiplier *= CurrentWeapon.Config.fireMoveSpeedMultiplier;

            return multiplier;
        }

        /// <summary>
        /// 获取当前武器的瞄准灵敏度系数。
        /// </summary>
        public float GetCurrentAimSensitivityMultiplier()
        {
            if (CurrentWeapon == null || !IsAiming) return 1f;
            return CurrentWeapon.Config.aimSensitivityMultiplier;
        }

        /// <summary>
        /// 获取当前武器的辅助瞄准是否启用。
        /// </summary>
        public bool IsAimAssistEnabled()
        {
            return CurrentWeapon != null && CurrentWeapon.Config.aimAssistEnabled;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// GM：直接装备并切换到指定武器。
        /// </summary>
        public void GM_EquipAndSwitch(int weaponConfigId)
        {
            EquipWeapon(_currentSlot, weaponConfigId);
            SwitchToSlot(_currentSlot);
        }
#endif
    }
}
