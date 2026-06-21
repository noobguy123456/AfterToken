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
        [SerializeField] private float _aimAssistRadius = 2f;
        [SerializeField] private float _aimAssistMaxAngle = 15f;
        [SerializeField] private LayerMask _enemyLayer;

        [Header("火箭锁定")]
        [SerializeField] private float _lockOnRange = 20f;
        [SerializeField] private float _lockOnAngle = 10f;
        [SerializeField] private float _lockOnHoldTime = 1.5f;

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

            var target = FindBestAssistTarget(origin, direction);
            if (target == null) return direction;

            Vector2 toTarget = ((Vector2)target.position - origin).normalized;
            float angle = Vector2.Angle(direction, toTarget);
            if (angle > _aimAssistMaxAngle) return direction;

            float t = 1 - (angle / _aimAssistMaxAngle);
            return Vector2.Lerp(direction, toTarget, t * 0.5f).normalized;
        }

        private Transform FindBestAssistTarget(Vector2 origin, Vector2 direction)
        {
            var enemies = GameObject.FindObjectsByType<EnemyEntity>(FindObjectsSortMode.None);
            Transform best = null;
            float bestScore = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;

                Vector2 toEnemy = (Vector2)enemy.transform.position - origin;
                float distance = toEnemy.magnitude;
                if (distance > _aimAssistRadius) continue;

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

            Vector2 origin = player.transform.position;
            Vector2 aimDir = ((Vector2)player.AimPosition - origin).normalized;

            var target = FindLockOnTarget(origin, aimDir);
            if (target == null)
            {
                ClearLockOn();
                return;
            }

            if (_lockedTarget == target)
            {
                _lockTimer += Time.deltaTime;
                if (!_isLocked && _lockTimer >= _lockOnHoldTime)
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

        private Transform FindLockOnTarget(Vector2 origin, Vector2 direction)
        {
            var enemies = GameObject.FindObjectsByType<EnemyEntity>(FindObjectsSortMode.None);
            Transform best = null;
            float bestAngle = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;

                Vector2 toEnemy = (Vector2)enemy.transform.position - origin;
                float distance = toEnemy.magnitude;
                if (distance > _lockOnRange) continue;

                float angle = Vector2.Angle(direction, toEnemy.normalized);
                if (angle > _lockOnAngle) continue;

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
