using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 武器配置（临时实现，后续由 Luban 生成替代）。
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

        // 后坐力/抖动
        public float recoilIntensity;

        // 资源
        public string muzzleEffect;
        public string hitEffect;
        public string fireSound;
        public string reloadSound;
    }
}
