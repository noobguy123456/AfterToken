using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 武器配置管理器（临时实现，后续接入 Luban 配置表）。
    /// </summary>
    public class WeaponConfigMgr
    {
        private static WeaponConfigMgr _instance;
        public static WeaponConfigMgr Instance => _instance ??= new WeaponConfigMgr();

        private readonly Dictionary<int, WeaponConfig> _configs = new Dictionary<int, WeaponConfig>();

        private WeaponConfigMgr()
        {
            // 临时数据，用于测试
            _configs[1001] = new WeaponConfig
            {
                id = 1001,
                name = "Pistol",
                weaponType = WeaponType.Pistol,
                ballisticType = BallisticType.Raycast,
                fireMode = FireMode.Single,
                damage = 15,
                fireRate = 5,
                clipSize = 12,
                reloadTime = 1.2f,
                maxRange = 20,
                baseSpread = 3,
                moveSpreadMultiplier = 1.5f,
                fireSpreadIncrement = 1,
                spreadRecoveryRate = 5,
                aimSpreadMultiplier = 0.5f,
                moveSpeedMultiplier = 1,
                fireMoveSpeedMultiplier = 0.9f,
                aimFov = 50,
                aimSensitivityMultiplier = 0.8f,
                aimAssistEnabled = true,
                tracerSpeed = 40,
                tracerDelay = 0,
                recoilIntensity = 0.5f,
                showDebugRay = true,
                debugRayDuration = 1f,
            };

            _configs[1002] = new WeaponConfig
            {
                id = 1002,
                name = "SMG",
                weaponType = WeaponType.SMG,
                ballisticType = BallisticType.Raycast,
                fireMode = FireMode.Auto,
                damage = 8,
                fireRate = 12,
                clipSize = 30,
                reloadTime = 1.5f,
                maxRange = 15,
                baseSpread = 5,
                moveSpreadMultiplier = 2,
                fireSpreadIncrement = 1.5f,
                spreadRecoveryRate = 8,
                aimSpreadMultiplier = 0.4f,
                moveSpeedMultiplier = 1,
                fireMoveSpeedMultiplier = 0.85f,
                aimFov = 45,
                aimSensitivityMultiplier = 0.7f,
                aimAssistEnabled = true,
                tracerSpeed = 50,
                tracerDelay = 0,
                recoilIntensity = 0.3f,
                showDebugRay = true,
                debugRayDuration = 1f,
            };

            _configs[1003] = new WeaponConfig
            {
                id = 1003,
                name = "Rifle",
                weaponType = WeaponType.Rifle,
                ballisticType = BallisticType.Raycast,
                fireMode = FireMode.Auto,
                damage = 12,
                fireRate = 8,
                clipSize = 25,
                reloadTime = 1.8f,
                maxRange = 25,
                baseSpread = 2,
                moveSpreadMultiplier = 1.8f,
                fireSpreadIncrement = 1.2f,
                spreadRecoveryRate = 6,
                aimSpreadMultiplier = 0.3f,
                moveSpeedMultiplier = 0.95f,
                fireMoveSpeedMultiplier = 0.8f,
                aimFov = 40,
                aimSensitivityMultiplier = 0.6f,
                aimAssistEnabled = true,
                tracerSpeed = 60,
                tracerDelay = 0,
                recoilIntensity = 0.6f,
                showDebugRay = true,
                debugRayDuration = 1f,
            };

            _configs[1004] = new WeaponConfig
            {
                id = 1004,
                name = "Sniper",
                weaponType = WeaponType.Sniper,
                ballisticType = BallisticType.Raycast,
                fireMode = FireMode.Single,
                damage = 80,
                fireRate = 1,
                clipSize = 5,
                reloadTime = 2.5f,
                maxRange = 50,
                baseSpread = 0.5f,
                moveSpreadMultiplier = 3,
                fireSpreadIncrement = 5,
                spreadRecoveryRate = 3,
                aimSpreadMultiplier = 0.1f,
                moveSpeedMultiplier = 0.7f,
                fireMoveSpeedMultiplier = 0.5f,
                aimFov = 25,
                aimSensitivityMultiplier = 0.4f,
                aimAssistEnabled = false,
                tracerSpeed = 100,
                tracerDelay = 0,
                recoilIntensity = 1.5f,
                showDebugRay = true,
                debugRayDuration = 1f,
            };

            _configs[1005] = new WeaponConfig
            {
                id = 1005,
                name = "Rocket Launcher",
                weaponType = WeaponType.Rocket,
                ballisticType = BallisticType.Projectile,
                fireMode = FireMode.Single,
                damage = 100,
                fireRate = 1,
                clipSize = 1,
                reloadTime = 3,
                maxRange = 30,
                baseSpread = 1,
                moveSpreadMultiplier = 1.2f,
                fireSpreadIncrement = 0,
                spreadRecoveryRate = 10,
                aimSpreadMultiplier = 0.5f,
                moveSpeedMultiplier = 0.6f,
                fireMoveSpeedMultiplier = 0.4f,
                aimFov = 35,
                aimSensitivityMultiplier = 0.5f,
                aimAssistEnabled = false,
                projectileSpeed = 10,
                projectileLifeTime = 5,
                projectilePrefab = "Projectile_Rocket",
                explosionRadius = 3,
                explosionDamageFalloff = 0.5f,
                trackingTime = 1.5f,
                trackingLaserColor = Color.red,
                lockOnSound = "SFX_LockOn",
                recoilIntensity = 2,
                showDebugRay = true,
                debugRayDuration = 1f,
            };
        }

        public WeaponConfig Get(int id)
        {
            _configs.TryGetValue(id, out var config);
            return config;
        }

        public IEnumerable<WeaponConfig> GetAll() => _configs.Values;
    }
}
