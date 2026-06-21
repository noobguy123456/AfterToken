using System.Collections.Generic;
using UnityEngine;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 弹道系统。
    /// 根据武器配置处理 Raycast 即时命中与 Projectile 飞行物。
    /// </summary>
    public class BallisticSystem : MonoBehaviour
    {
        public static BallisticSystem Instance { get; private set; }

        [Header("命中检测")]
        [SerializeField] private LayerMask _hitLayers;
        [SerializeField] private float _tracerRadius = 0.05f;

        [Header("瞄准辅助可视化")]
        [SerializeField] private LineRenderer _lockOnLaserPrefab;

        private readonly GameEventMgr _eventMgr = new GameEventMgr();
        private readonly List<TracerVisual> _activeTracers = new List<TracerVisual>();
        private Transform _tracerRoot;
        private LineRenderer _rocketLaser;

        private void Awake()
        {
            Instance = this;

            if (_hitLayers == 0)
            {
                _hitLayers = LayerMask.GetMask("Enemy", "Obstacle");
            }

            _eventMgr.AddEvent<Vector2, Vector2, int, int>(IWeaponEvent_Event.OnFire, OnFire);

            var root = new GameObject("TracerRoot");
            root.transform.SetParent(transform, false);
            _tracerRoot = root.transform;
        }

        private void OnDestroy()
        {
            _eventMgr.Clear();
            Instance = null;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = _activeTracers.Count - 1; i >= 0; i--)
            {
                var tracer = _activeTracers[i];
                if (tracer == null)
                {
                    _activeTracers.RemoveAt(i);
                    continue;
                }

                tracer.LifeTime -= deltaTime;
                if (tracer.LifeTime <= 0)
                {
                    Destroy(tracer.GameObject);
                    _activeTracers.RemoveAt(i);
                    continue;
                }

                tracer.Position += tracer.Direction * tracer.Speed * deltaTime;
                tracer.Transform.position = tracer.Position;

                // 接近命中点时结束
                if ((tracer.Position - tracer.HitPoint).sqrMagnitude < 0.01f)
                {
                    Destroy(tracer.GameObject);
                    _activeTracers.RemoveAt(i);
                }
            }

            UpdateRocketLaser();
        }

        private void OnFire(Vector2 origin, Vector2 direction, int weaponConfigId, int ownerId)
        {
            var config = WeaponConfigMgr.Instance?.Get(weaponConfigId);
            if (config == null) return;

            if (config.ballisticType == BallisticType.Raycast)
            {
                FireRaycast(origin, direction, config, ownerId);
            }
            else if (config.ballisticType == BallisticType.Projectile)
            {
                FireProjectile(origin, direction, config, ownerId);
            }
        }

        private void FireRaycast(Vector2 origin, Vector2 direction, WeaponConfig config, int ownerId)
        {
            float maxDistance = config.maxRange;
            float radius = config.raycastRadius >= 0 ? config.raycastRadius : _tracerRadius;
            LayerMask layers = config.hitLayers != 0 ? config.hitLayers : _hitLayers;

            RaycastHit2D hit = Physics2D.CircleCast(origin, radius, direction, maxDistance, layers);

            Vector2 hitPoint = origin + direction * maxDistance;
            GameObject hitTarget = null;
            bool hasHit = hit.collider != null;
            if (hasHit)
            {
                hitPoint = hit.point;
                hitTarget = hit.collider.gameObject;

                // 立即伤害（命中反馈统一由 BattleSystem 触发，避免重复）
                var damageInfo = MemoryPool.Acquire<DamageInfo>();
                damageInfo.AttackerId = ownerId;
                damageInfo.WeaponConfigId = config.id;
                damageInfo.TargetGameObject = hitTarget;
                damageInfo.Damage = config.damage;
                damageInfo.HitDirection = (hitPoint - origin).normalized;
                damageInfo.HitPoint = hitPoint;
                GameEvent.Get<IBattleEvent>().OnEntityDamaged(damageInfo);
            }

            // Debug 可视化
            if (config.showDebugRay)
            {
                DrawDebugRaycast(origin, direction, hitPoint, maxDistance, hasHit, config);
            }

            // 延迟 tracer 视觉
            SpawnTracer(origin, hitPoint, direction, config);

            // 枪口特效（占位）
            // SpawnMuzzleEffect(origin, config);
        }

        private void DrawDebugRaycast(Vector2 origin, Vector2 direction, Vector2 hitPoint, float maxDistance, bool hasHit, WeaponConfig config)
        {
            Color color = hasHit ? config.debugHitColor : config.debugMissColor;
            Vector2 endPoint = hasHit ? hitPoint : origin + direction * maxDistance;

            // 主射线
            Debug.DrawLine(origin, endPoint, color, config.debugRayDuration);

            if (hasHit)
            {
                // 命中点十字标记
                Debug.DrawRay(hitPoint, Vector2.up * 0.2f, color, config.debugRayDuration);
                Debug.DrawRay(hitPoint, Vector2.down * 0.2f, color, config.debugRayDuration);
                Debug.DrawRay(hitPoint, Vector2.left * 0.2f, color, config.debugRayDuration);
                Debug.DrawRay(hitPoint, Vector2.right * 0.2f, color, config.debugRayDuration);
            }
        }

        private void FireProjectile(Vector2 origin, Vector2 direction, WeaponConfig config, int ownerId)
        {
            int targetId = 0;
            bool tracking = false;

            if (config.weaponType == WeaponType.Rocket)
            {
                var lockTarget = AimAssistSystem.Instance?.GetLockedTarget();
                if (lockTarget != null)
                {
                    targetId = lockTarget.GetInstanceID();
                    tracking = true;
                }
            }

            ProjectileSystem.Instance?.CreateProjectile(config.id, ownerId, origin, direction, targetId, tracking);
        }

        private void SpawnTracer(Vector2 origin, Vector2 hitPoint, Vector2 direction, WeaponConfig config)
        {
            float distance = Vector2.Distance(origin, hitPoint);
            float speed = config.tracerSpeed > 0 ? config.tracerSpeed : 50f;
            float delay = config.tracerDelay;
            float lifeTime = distance / speed + delay;

            var go = new GameObject("Tracer");
            go.transform.SetParent(_tracerRoot, false);
            go.transform.position = origin;

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.02f;
            lr.positionCount = 2;
            lr.SetPosition(0, origin);
            lr.SetPosition(1, origin + direction * 0.1f);
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.yellow;
            lr.endColor = Color.yellow;

            _activeTracers.Add(new TracerVisual
            {
                GameObject = go,
                Transform = go.transform,
                Position = origin,
                Direction = direction,
                HitPoint = hitPoint,
                Speed = speed,
                LifeTime = lifeTime,
                LineRenderer = lr,
            });
        }

        private void UpdateRocketLaser()
        {
            var target = AimAssistSystem.Instance?.GetLockedTarget();
            if (target == null)
            {
                if (_rocketLaser != null) _rocketLaser.enabled = false;
                return;
            }

            var player = PlayerSystem.Instance?.GetPlayerEntity();
            if (player == null) return;

            if (_rocketLaser == null && _lockOnLaserPrefab != null)
            {
                _rocketLaser = Instantiate(_lockOnLaserPrefab);
            }

            if (_rocketLaser != null)
            {
                _rocketLaser.enabled = true;
                _rocketLaser.SetPosition(0, player.transform.position);
                _rocketLaser.SetPosition(1, target.transform.position);
                _rocketLaser.startColor = Color.red;
                _rocketLaser.endColor = Color.red;
            }
        }

        private class TracerVisual
        {
            public GameObject GameObject;
            public Transform Transform;
            public Vector2 Position;
            public Vector2 Direction;
            public Vector2 HitPoint;
            public float Speed;
            public float LifeTime;
            public LineRenderer LineRenderer;
        }
    }
}
