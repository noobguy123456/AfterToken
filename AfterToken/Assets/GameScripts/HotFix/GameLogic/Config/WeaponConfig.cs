using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 武器配置（对 Luban 生成配置的运行时适配）。
    /// </summary>
    public class WeaponConfig
    {
        public int id;
        public string name;
        public WeaponType weaponType;
        public BallisticType ballisticType;
        public FireMode fireMode;

        // 基础属性
        public float damage;
        public float fireRate;
        public int clipSize;
        public float reloadTime;
        public float maxRange;

        // 扩散
        public float baseSpread;
        public float moveSpreadMultiplier;
        public float fireSpreadIncrement;
        public float spreadRecoveryRate;
        public float aimSpreadMultiplier;

        // 移动
        public float moveSpeedMultiplier;
        public float fireMoveSpeedMultiplier;

        // 瞄准
        public float aimFov;
        public float aimSensitivityMultiplier;
        public bool aimAssistEnabled;

        // 弹道
        public float tracerSpeed;
        public float tracerDelay;
        public float projectileSpeed;
        public float projectileLifeTime;
        public string projectilePrefab;
        public float explosionRadius;
        public float explosionDamageFalloff;

        // 射线检测（即时命中武器）
        [Header("射线检测")]
        public float raycastRadius = -1f;               // < 0 时使用 BallisticSystem 全局默认值
        public LayerMask hitLayers = 0;                 // 0 时使用 BallisticSystem 全局默认值

        [Header("Debug 可视化")]
        public bool showDebugRay = true;
        public float debugRayDuration = 1f;
        public Color debugHitColor = Color.green;
        public Color debugMissColor = Color.red;

        // 火箭炮锁定
        public float trackingTime;
        public Color trackingLaserColor;
        public string lockOnSound;

        // 辅助瞄准
        public float aimAssistRadius;
        public float aimAssistMaxAngle;
        public float lockOnRange;
        public float lockOnAngle;
        public float lockOnHoldTime;

        // 后坐力/抖动
        public float recoilIntensity;

        // 资源
        public string muzzleEffect;
        public string hitEffect;
        public string fireSound;
        public string reloadSound;

        public WeaponConfig() { }

        public WeaponConfig(GameConfig.cfg.Weapon w)
        {
            id = w.Id;
            name = w.Name;
            weaponType = (WeaponType)w.WeaponType;
            ballisticType = (BallisticType)w.BallisticType;
            fireMode = (FireMode)w.FireMode;
            damage = w.Damage;
            fireRate = w.FireRate;
            clipSize = w.ClipSize;
            reloadTime = w.ReloadTime;
            maxRange = w.MaxRange;
            baseSpread = w.BaseSpread;
            moveSpreadMultiplier = w.MoveSpreadMultiplier;
            fireSpreadIncrement = w.FireSpreadIncrement;
            spreadRecoveryRate = w.SpreadRecoveryRate;
            aimSpreadMultiplier = w.AimSpreadMultiplier;
            moveSpeedMultiplier = w.MoveSpeedMultiplier;
            fireMoveSpeedMultiplier = w.FireMoveSpeedMultiplier;
            aimFov = w.AimFov;
            aimSensitivityMultiplier = w.AimSensitivityMultiplier;
            aimAssistEnabled = w.AimAssistEnabled;
            tracerSpeed = w.TracerSpeed;
            tracerDelay = w.TracerDelay;
            projectileSpeed = w.ProjectileSpeed;
            projectileLifeTime = w.ProjectileLifeTime;
            projectilePrefab = w.ProjectilePrefab;
            explosionRadius = w.ExplosionRadius;
            explosionDamageFalloff = w.ExplosionDamageFalloff;
            recoilIntensity = w.RecoilIntensity;
            raycastRadius = w.RaycastRadius;
            hitLayers = ParseLayerMask(w.HitLayers);
            showDebugRay = w.ShowDebugRay;
            debugRayDuration = w.DebugRayDuration;
            debugHitColor = ToUnityColor(w.DebugHitColor);
            debugMissColor = ToUnityColor(w.DebugMissColor);
            trackingTime = w.TrackingTime;
            trackingLaserColor = ToUnityColor(w.TrackingLaserColor);
            lockOnSound = w.LockOnSound;
            aimAssistRadius = w.AimAssistRadius;
            aimAssistMaxAngle = w.AimAssistMaxAngle;
            lockOnRange = w.LockOnRange;
            lockOnAngle = w.LockOnAngle;
            lockOnHoldTime = w.LockOnHoldTime;
            muzzleEffect = w.MuzzleEffect;
            hitEffect = w.HitEffect;
            fireSound = w.FireSound;
            reloadSound = w.ReloadSound;
        }

        private static LayerMask ParseLayerMask(string layers)
        {
            if (string.IsNullOrWhiteSpace(layers)) return 0;
            var mask = 0;
            foreach (var part in layers.Split(',', ';'))
            {
                var layerName = part.Trim();
                if (string.IsNullOrEmpty(layerName)) continue;
                var layer = LayerMask.NameToLayer(layerName);
                if (layer >= 0) mask |= 1 << layer;
            }
            return mask;
        }

        private static Color ToUnityColor(GameConfig.cfg.Color c)
        {
            if (c == null) return Color.white;
            return new Color(c.R, c.G, c.B, c.A);
        }
    }
}
