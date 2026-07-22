using UnityEngine;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 辅助瞄准系统。
    /// 处理轻微磁吸与火箭锁定。
    /// </summary>
    public class AimAssistSystem : MonoBehaviour
    {
        public static AimAssistSystem Instance { get; private set; }

        [Header("辅助瞄准")]
        [SerializeField] private float _aimAssistRadius;
        [SerializeField] private float _aimAssistMaxAngle;
        [SerializeField] private LayerMask _enemyLayer;

        [Header("火箭锁定")]
        [SerializeField] private float _lockOnRange;
        [SerializeField] private float _lockOnAngle;
        [SerializeField] private float _lockOnHoldTime;

        // 当武器配置未提供辅助瞄准参数时的兜底值。
        private const float DEFAULT_AIM_ASSIST_RADIUS = 2f;
        private const float DEFAULT_AIM_ASSIST_MAX_ANGLE = 15f;
        private const float DEFAULT_LOCK_ON_RANGE = 20f;
        private const float DEFAULT_LOCK_ON_ANGLE = 10f;
        private const float DEFAULT_LOCK_ON_HOLD_TIME = 1.5f;

        private readonly GameEventMgr _eventMgr = new GameEventMgr();

        private Transform _lockedTarget;
        private float _lockTimer;
        private bool _isLocked;
        private bool _isAiming;

        private void Awake()
        {
            Instance = this;

            _eventMgr.AddEvent<int, bool>(IWeaponEvent_Event.OnAimStateChanged, OnAimStateChanged);
        }

        private void OnDestroy()
        {
            _eventMgr.Clear();
            Instance = null;
        }

        private void Update()
        {
            if (_isAiming && WeaponSystem.Instance?.CurrentWeapon?.Config.weaponType == WeaponType.Rocket)
            {
                UpdateRocketLockOn();
            }
            else
            {
                ClearLockOn();
            }
        }

        private void OnAimStateChanged(int ownerId, bool isAiming)
        {
            _isAiming = isAiming;
            if (!_isAiming)
            {
                ClearLockOn();
            }
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

            Vector2 toTarget = ((Vector2)target.position - origin).normalized;
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

                Vector2 toEnemy = (Vector2)enemy.transform.position - origin;
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

        private void UpdateRocketLockOn()
        {
            var player = PlayerSystem.Instance?.GetPlayerEntity();
            if (player == null) return;

            var weaponConfig = WeaponSystem.Instance?.CurrentWeapon?.Config;
            if (weaponConfig == null)
            {
                ClearLockOn();
                return;
            }

            Vector2 origin = player.transform.position;
            Vector2 aimDir = ((Vector2)player.AimPosition - origin).normalized;

            var target = FindLockOnTarget(origin, aimDir, weaponConfig);
            if (target == null)
            {
                ClearLockOn();
                return;
            }

            float holdTime = weaponConfig.lockOnHoldTime > 0 ? weaponConfig.lockOnHoldTime : DEFAULT_LOCK_ON_HOLD_TIME;

            if (_lockedTarget == target)
            {
                _lockTimer += Time.deltaTime;
                if (!_isLocked && _lockTimer >= holdTime)
                {
                    _isLocked = true;
                    GameEvent.Get<IHitFeedbackEvent>()?.OnTargetLocked(target.GetInstanceID());
                }
            }
            else
            {
                _lockedTarget = target;
                _lockTimer = 0;
                _isLocked = false;
            }
        }

        private Transform FindLockOnTarget(Vector2 origin, Vector2 direction, WeaponConfig config)
        {
            var enemies = EnemyRegistry.All;
            Transform best = null;
            float bestAngle = float.MaxValue;
            float range = config?.lockOnRange > 0 ? config.lockOnRange : DEFAULT_LOCK_ON_RANGE;
            float angleLimit = config?.lockOnAngle > 0 ? config.lockOnAngle : DEFAULT_LOCK_ON_ANGLE;

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;

                Vector2 toEnemy = (Vector2)enemy.transform.position - origin;
                float distance = toEnemy.magnitude;
                if (distance > range) continue;

                float angle = Vector2.Angle(direction, toEnemy.normalized);
                if (angle > angleLimit) continue;

                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    best = enemy.transform;
                }
            }

            return best;
        }

        private void ClearLockOn()
        {
            _lockedTarget = null;
            _lockTimer = 0;
            _isLocked = false;
        }

        public Transform GetLockedTarget()
        {
            return _isLocked ? _lockedTarget : null;
        }
    }
}
