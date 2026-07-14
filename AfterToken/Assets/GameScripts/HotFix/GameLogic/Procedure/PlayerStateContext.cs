using UnityEngine;

namespace GameLogic.Portal
{
    /// <summary>
    /// 传送门玩家状态快照。
    /// 用于在传送门触发时保存玩家状态，并在目标场景加载后恢复。
    /// </summary>
    public static class PortalPlayerState
    {
        /// <summary>
        /// 是否已保存状态。
        /// </summary>
        public static bool HasSavedState { get; private set; }

        /// <summary>
        /// 当前血量。
        /// </summary>
        public static int Hp { get; private set; }

        /// <summary>
        /// 最大血量。
        /// </summary>
        public static int MaxHp { get; private set; }

        /// <summary>
        /// 当前体力。
        /// </summary>
        public static int Stamina { get; private set; }

        /// <summary>
        /// 最大体力。
        /// </summary>
        public static int MaxStamina { get; private set; }

        /// <summary>
        /// 武器状态数据。
        /// </summary>
        public static WeaponStateData[] Weapons { get; private set; }

        /// <summary>
        /// 当前武器槽位。
        /// </summary>
        public static int CurrentWeaponSlot { get; private set; }

        /// <summary>
        /// 保存玩家状态。
        /// </summary>
        public static void Save(PlayerSystem playerSystem, WeaponSystem weaponSystem)
        {
            if (playerSystem == null || weaponSystem == null)
            {
                Debug.LogWarning("[PortalPlayerState] Save failed: playerSystem or weaponSystem is null.");
                return;
            }

            Hp = playerSystem.CurrentHp;
            MaxHp = playerSystem.MaxHp;
            Stamina = playerSystem.CurrentStamina;
            MaxStamina = playerSystem.MaxStamina;
            CurrentWeaponSlot = weaponSystem.CurrentSlotIndex;

            Weapons = new WeaponStateData[WeaponSystem.MAX_WEAPON_SLOTS];
            for (int i = 0; i < WeaponSystem.MAX_WEAPON_SLOTS; i++)
            {
                var weapon = weaponSystem.GetWeaponInSlot(i);
                if (weapon != null && weapon.Config != null)
                {
                    Weapons[i] = new WeaponStateData
                    {
                        ConfigId = weapon.Config.id,
                        CurrentAmmo = weapon.CurrentAmmo
                    };
                }
                else
                {
                    Weapons[i] = WeaponStateData.Empty;
                }
            }

            HasSavedState = true;
        }

        /// <summary>
        /// 恢复玩家状态（血量与体力）。
        /// 武器状态由 WeaponSystem.Start 在检测到保存状态时自动恢复。
        /// </summary>
        public static void Restore(PlayerSystem playerSystem)
        {
            if (!HasSavedState) return;

            if (playerSystem == null)
            {
                Debug.LogWarning("[PortalPlayerState] Restore failed: playerSystem is null.");
                return;
            }

            playerSystem.RestoreHpAndStamina(Hp, MaxHp, Stamina, MaxStamina);
            Clear();
        }

        /// <summary>
        /// 清除保存的状态。
        /// </summary>
        public static void Clear()
        {
            HasSavedState = false;
            Hp = 0;
            MaxHp = 0;
            Stamina = 0;
            MaxStamina = 0;
            Weapons = null;
            CurrentWeaponSlot = 0;
        }
    }

    /// <summary>
    /// 武器状态数据。
    /// </summary>
    public struct WeaponStateData
    {
        public static WeaponStateData Empty => new WeaponStateData { ConfigId = 0, CurrentAmmo = 0 };

        public int ConfigId;
        public int CurrentAmmo;

        public bool IsValid => ConfigId > 0;
    }
}
