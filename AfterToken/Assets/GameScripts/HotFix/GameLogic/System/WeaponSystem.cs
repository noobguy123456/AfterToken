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

        [Header("武器切换")]
        [SerializeField] private float _weaponSwitchCooldown = 0.3f;

        [Header("瞄准设置")]
        [SerializeField] private AimMode _aimMode = AimMode.Hold;

        public WeaponInstance[] Slots { get; private set; }
        private int _currentSlot = 0;
        private IWeaponOwner _owner;
        private int[] _defaultWeaponIds;
        private bool _isAiming;
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
                weapon.Tick(Time.deltaTime, _owner?.IsMoving ?? false, _isAiming);
            }

            if (_firePending || (_isFiring && weapon != null && weapon.Config.fireMode == FireMode.Auto))
            {
                TryFire();
            }
        }

        public WeaponInstance CurrentWeapon => Slots[_currentSlot];
        public int CurrentSlotIndex => _currentSlot;
        public bool IsAiming => _isAiming;
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

            _currentSlot = slot;
            _lastSwitchTime = Time.time;
            _isFiring = false;
            _firePending = false;

            GameEvent.Get<IWeaponEvent>().OnWeaponSwitched(_owner?.OwnerId ?? 0, slot);
            GameEvent.Get<IPlayerEvent>().OnAmmoChanged(
                CurrentWeapon?.CurrentAmmo ?? 0,
                CurrentWeapon?.Config.clipSize ?? 0);

            // 切换武器时取消瞄准
            if (_isAiming)
            {
                SetAimState(false);
            }
        }

        private bool CanSwitch()
        {
            return Time.time - _lastSwitchTime >= _weaponSwitchCooldown;
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
            if (_aimMode == AimMode.Hold)
            {
                SetAimState(true);
            }
            else
            {
                SetAimState(!_isAiming);
            }
        }

        private void OnAimReleased()
        {
            if (_aimMode == AimMode.Hold)
            {
                SetAimState(false);
            }
        }

        private void SetAimState(bool aiming)
        {
            if (_isAiming == aiming) return;
            _isAiming = aiming;

            GameEvent.Get<IWeaponEvent>().OnAimStateChanged(_owner?.OwnerId ?? 0, _isAiming);

            // 更新相机 FOV
            float targetFov = _isAiming && CurrentWeapon != null
                ? CurrentWeapon.Config.aimFov
                : 60f;
            GameEvent.Get<ICameraEvent>().OnAimFovChanged(targetFov);

            // 狙击枪开镜时打开瞄准镜 UI
            bool isSniper = CurrentWeapon?.Config.weaponType == WeaponType.Sniper;
            if (isSniper)
            {
                if (_isAiming)
                    GameModule.UI.ShowUIAsync<SniperScopeUI>();
                else
                    GameModule.UI.CloseUI<SniperScopeUI>();
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

            // 辅助瞄准修正
            Vector2 direction = AimAssistSystem.Instance?.ApplyAimAssist(
                origin,
                rawDirection,
                weapon.Config.id,
                _isAiming) ?? rawDirection;

            // 扩散
            float spread = weapon.CalculateSpread(
                _owner.IsMoving,
                _isAiming);
            direction = ApplySpread(direction, spread);

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
            if (CurrentWeapon == null || !_isAiming) return 1f;
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
