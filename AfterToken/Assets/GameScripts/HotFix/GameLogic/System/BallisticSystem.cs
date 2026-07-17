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

        [Header("Tracer 表现")]
        [SerializeField] private float _tracerStartWidth = 0.1f;
        [SerializeField] private float _tracerEndWidth = 0.05f;
        [SerializeField] private float _tracerTailLength = 0.5f;
        [SerializeField] private int _maxActiveTracers = 30;

        private readonly GameEventMgr _eventMgr = new GameEventMgr();
        private readonly List<TracerVisual> _activeTracers = new List<TracerVisual>();
        private readonly Queue<TracerVisual> _tracerPool = new Queue<TracerVisual>();
        private Transform _tracerRoot;
        private LineRenderer _rocketLaser;
        private Material _tracerMaterial;

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

            _tracerMaterial = new Material(Shader.Find("Sprites/Default"));

            InitializeRocketLaser();
        }

        private void InitializeRocketLaser()
        {
            if (_lockOnLaserPrefab == null) return;

            _rocketLaser = Instantiate(_lockOnLaserPrefab, transform);
            _rocketLaser.enabled = false;
        }

        private void OnDestroy()
        {
            _eventMgr.Clear();
            Instance = null;

            foreach (var tracer in _activeTracers)
                DestroyTracer(tracer);
            _activeTracers.Clear();

            while (_tracerPool.Count > 0)
            {
                var tracer = _tracerPool.Dequeue();
                if (tracer?.GameObject != null)
                    Destroy(tracer.GameObject);
            }

            if (_tracerMaterial != null)
                Destroy(_tracerMaterial);

            if (_rocketLaser != null)
                Destroy(_rocketLaser.gameObject);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = _activeTracers.Count - 1; i >= 0; i--)
            {
                var tracer = _activeTracers[i];
                if (tracer == null || tracer.GameObject == null)
                {
                    _activeTracers.RemoveAt(i);
                    continue;
                }

                tracer.LifeTime -= deltaTime;
                if (tracer.LifeTime <= 0)
                {
                    ReleaseTracer(tracer);
                    _activeTracers.RemoveAt(i);
                    continue;
                }

                tracer.Position += tracer.Direction * tracer.Speed * deltaTime;
                tracer.Transform.position = tracer.Position;

                // 更新 tracer 拖尾线段
                if (tracer.LineRenderer != null)
                {
                    float tailLength = Mathf.Min(_tracerTailLength, Vector2.Distance(tracer.Position, tracer.HitPoint));
                    tracer.LineRenderer.SetPosition(0, tracer.Position);
                    tracer.LineRenderer.SetPosition(1, tracer.Position - tracer.Direction * tailLength);
                }

                // 接近命中点时结束
                if ((tracer.Position - tracer.HitPoint).sqrMagnitude < 0.01f)
                {
                    ReleaseTracer(tracer);
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

            // 限制最大同时存在的 tracer 数量，回收最旧的
            if (_activeTracers.Count >= _maxActiveTracers)
            {
                var oldest = _activeTracers[0];
                ReleaseTracer(oldest);
                _activeTracers.RemoveAt(0);
            }

            var tracer = AcquireTracer();
            tracer.Position = origin;
            tracer.Direction = direction;
            tracer.HitPoint = hitPoint;
            tracer.Speed = speed;
            tracer.LifeTime = lifeTime;

            tracer.GameObject.SetActive(true);
            tracer.Transform.position = origin;

            if (tracer.LineRenderer != null)
            {
                tracer.LineRenderer.SetPosition(0, origin);
                tracer.LineRenderer.SetPosition(1, origin + direction * Mathf.Min(_tracerTailLength, distance));
            }

            _activeTracers.Add(tracer);
        }

        private TracerVisual AcquireTracer()
        {
            if (_tracerPool.Count > 0)
                return _tracerPool.Dequeue();

            var go = new GameObject("Tracer");
            go.transform.SetParent(_tracerRoot, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = _tracerStartWidth;
            lr.endWidth = _tracerEndWidth;
            lr.positionCount = 2;
            lr.material = _tracerMaterial;
            lr.startColor = new Color(1f, 0.9f, 0.2f, 0.9f);
            lr.endColor = new Color(1f, 0.6f, 0f, 0.3f);

            return new TracerVisual
            {
                GameObject = go,
                Transform = go.transform,
                LineRenderer = lr,
            };
        }

        private void ReleaseTracer(TracerVisual tracer)
        {
            if (tracer == null) return;
            if (tracer.GameObject != null)
                tracer.GameObject.SetActive(false);
            _tracerPool.Enqueue(tracer);
        }

        private void DestroyTracer(TracerVisual tracer)
        {
            if (tracer?.GameObject != null)
                Destroy(tracer.GameObject);
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
