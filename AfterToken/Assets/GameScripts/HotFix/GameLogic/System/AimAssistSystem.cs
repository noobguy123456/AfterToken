using UnityEngine;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 辅助瞄准系统。
    /// 处理轻微磁吸。
    /// （火箭筒自动索敌/锁定已移除：RPG 定位是直线爆炸物，激光仅为常态瞄准指示。）
    /// </summary>
    public class AimAssistSystem : MonoBehaviour
    {
        public static AimAssistSystem Instance { get; private set; }

        [Header("辅助瞄准")]
        [SerializeField] private float _aimAssistRadius;
        [SerializeField] private float _aimAssistMaxAngle;
        [SerializeField] private LayerMask _enemyLayer;

        // 当武器配置未提供辅助瞄准参数时的兜底值。
        private const float DEFAULT_AIM_ASSIST_RADIUS = 2f;
        private const float DEFAULT_AIM_ASSIST_MAX_ANGLE = 15f;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        /// <summary>
        /// 对方向应用辅助瞄准修正。
        /// </summary>
        public Vector2 ApplyAimAssist(Vector2 origin, Vector2 direction, int weaponConfigId, bool isAiming)
        {
            var config = WeaponConfigMgr.Instance?.Get(weaponConfigId);
            if (config == null || !config.aimAssistEnabled || !isAiming)
            {
                return direction;
            }

            var target = FindBestAssistTarget(origin, direction, config);
            if (target == null) return direction;

            Vector2 toTarget = (target.position.ToXZ() - origin).normalized;
            float angle = Vector2.Angle(direction, toTarget);
            if (angle > config.aimAssistMaxAngle) return direction;

            float t = 1 - (angle / config.aimAssistMaxAngle);
            return Vector2.Lerp(direction, toTarget, t * 0.5f).normalized;
        }

        private Transform FindBestAssistTarget(Vector2 origin, Vector2 direction, WeaponConfig config)
        {
            var enemies = EnemyRegistry.All;
            Transform best = null;
            float bestScore = float.MaxValue;
            float radius = config?.aimAssistRadius ?? DEFAULT_AIM_ASSIST_RADIUS;

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;

                Vector2 toEnemy = enemy.transform.position.ToXZ() - origin;
                float distance = toEnemy.magnitude;
                if (distance > radius) continue;

                float angle = Vector2.Angle(direction, toEnemy.normalized);
                float score = distance + angle * 0.1f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = enemy.transform;
                }
            }

            return best;
        }
    }
}
