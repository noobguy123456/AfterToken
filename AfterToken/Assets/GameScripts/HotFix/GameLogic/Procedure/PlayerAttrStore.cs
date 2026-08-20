using GameLogic.Portal;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家战斗属性运行态存储（血量/体力/武器弹药/当前槽位）。
    /// 变动即存：订阅 IPlayerEvent / IWeaponEvent，属性一变化立即写入本存储，
    /// 不再由传送门在转场瞬间快照（传送门只记录场景上下文，见 PortalPlayerState）。
    /// 内存态：一局结束（死亡/撤离/回大厅）时 Clear，不带入新一局。
    /// </summary>
    public static class PlayerAttrStore
    {
        /// <summary>
        /// 是否已有血量/体力数据（false 表示新一局或未初始化）。
        /// </summary>
        public static bool HasVitals { get; private set; }

        public static int Hp { get; private set; }
        public static int MaxHp { get; private set; }
        public static int Stamina { get; private set; }
        public static int MaxStamina { get; private set; }

        /// <summary>
        /// 各槽位武器状态；null 表示尚未记录过装备。
        /// </summary>
        public static WeaponStateData[] Weapons { get; private set; }

        /// <summary>
        /// 当前武器槽位。
        /// </summary>
        public static int CurrentWeaponSlot { get; private set; }

        /// <summary>
        /// 静态构造即订阅属性变化事件（进程级存储，与 GameEvent 同生命周期）。
        /// </summary>
        static PlayerAttrStore()
        {
            GameEvent.AddEventListener<int, int>(IPlayerEvent_Event.OnHpChanged, OnHpChanged);
            GameEvent.AddEventListener<int, int>(IPlayerEvent_Event.OnStaminaChanged, OnStaminaChanged);
            GameEvent.AddEventListener<int, int>(IPlayerEvent_Event.OnAmmoChanged, OnAmmoChanged);
            GameEvent.AddEventListener<int, int, int>(IWeaponEvent_Event.OnWeaponEquipped, OnWeaponEquipped);
            GameEvent.AddEventListener<int, int>(IWeaponEvent_Event.OnWeaponSwitched, OnWeaponSwitched);
        }

        private static void OnHpChanged(int currentHp, int maxHp)
        {
            Hp = currentHp;
            MaxHp = maxHp;
            HasVitals = true;
        }

        private static void OnStaminaChanged(int currentStamina, int maxStamina)
        {
            Stamina = currentStamina;
            MaxStamina = maxStamina;
            HasVitals = true;
        }

        private static void OnAmmoChanged(int currentAmmo, int maxAmmo)
        {
            // 弹药事件不带槽位：开火/换弹只发生在当前槽位武器上
            var weapons = Weapons;
            if (weapons == null || CurrentWeaponSlot >= weapons.Length) return;
            if (!weapons[CurrentWeaponSlot].IsValid) return;
            weapons[CurrentWeaponSlot].CurrentAmmo = currentAmmo;
        }

        private static void OnWeaponEquipped(int ownerId, int slot, int weaponConfigId)
        {
            Weapons ??= new WeaponStateData[WeaponSystem.MAX_WEAPON_SLOTS];
            if (slot < 0 || slot >= Weapons.Length) return;
            // 装备完成时实例已按满弹匣初始化，直接读回实际弹药
            int ammo = WeaponSystem.Instance?.GetWeaponInSlot(slot)?.CurrentAmmo ?? 0;
            Weapons[slot] = new WeaponStateData { ConfigId = weaponConfigId, CurrentAmmo = ammo };
        }

        private static void OnWeaponSwitched(int ownerId, int slot)
        {
            CurrentWeaponSlot = slot;
        }

        /// <summary>
        /// 直接写入某槽位武器状态（恢复路径专用：装备广播先于弹药回写，
        /// 靠事件会得到满弹匣的中间值，恢复方在 SetAmmo 后调用此方法修正）。
        /// </summary>
        public static void SetWeapon(int slot, int configId, int ammo)
        {
            Weapons ??= new WeaponStateData[WeaponSystem.MAX_WEAPON_SLOTS];
            if (slot < 0 || slot >= Weapons.Length) return;
            Weapons[slot] = new WeaponStateData { ConfigId = configId, CurrentAmmo = ammo };
        }

        /// <summary>
        /// 清空全部属性暂存（一局结束：死亡/撤离/回大厅）。
        /// </summary>
        public static void Clear()
        {
            HasVitals = false;
            Hp = 0;
            MaxHp = 0;
            Stamina = 0;
            MaxStamina = 0;
            Weapons = null;
            CurrentWeaponSlot = 0;
        }
    }
}
