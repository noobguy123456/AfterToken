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
        private float _tracerRadius;

        [Header("Tracer 表现")]
        private float _tracerStartWidth;
        private float _tracerEndWidth;
        private float _tracerTailLength;
        private int _maxActiveTracers;
        private Color _tracerStartColor;
        private Color _tracerEndColor;

        private readonly GameEventMgr _eventMgr = new GameEventMgr();
        private readonly List<TracerVisual> _activeTracers = new List<TracerVisual>();
        private readonly Queue<TracerVisual> _tracerPool = new Queue<TracerVisual>();
        private Transform _tracerRoot;
        private LineRenderer _rocketLaser;
        // 火箭瞄准激光的枪口组件缓存：按玩家实体缓存，避免每帧 GetComponent
        private PlayerEntity _mountViewOwner;
        private WeaponMountView _cachedMountView;
        // 常态瞄准激光颜色（暗红半透明）
        private static readonly Color LaserIdleColor = new Color(1f, 0.25f, 0.25f, 0.35f);
        private Material _tracerMaterial;

        // 弹道检测与弹迹表现的高度（地面 y=0 之上的视觉/检测层）。
        private const float BALLISTIC_HEIGHT = 0.5f;

        private void Awake()
        {
            Instance = this;

            LoadBallisticConfig();

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

        private void LoadBallisticConfig()
        {
            try
            {
                var ballisticConfig = ConfigSystem.Instance?.Tables?.TbBallistic?.GetOrDefault(1);
                if (ballisticConfig != null)
                {
                    _tracerRadius = ballisticConfig.TracerRadius;
                    _tracerStartWidth = ballisticConfig.TracerStartWidth;
                    _tracerEndWidth = ballisticConfig.TracerEndWidth;
                    _tracerTailLength = ballisticConfig.TracerTailLength;
                    _maxActiveTracers = ballisticConfig.MaxActiveTracers;
                    if (_hitLayers == 0 && !string.IsNullOrWhiteSpace(ballisticConfig.HitLayers))
                    {
                        _hitLayers = ParseLayerMask(ballisticConfig.HitLayers);
                    }
                    _tracerStartColor = ToUnityColor(ballisticConfig.TracerStartColor);
                    _tracerEndColor = ToUnityColor(ballisticConfig.TracerEndColor);
                    return;
                }
            }
            catch
            {
                // ignored
            }

            // 配置缺失时的兜底值。
            _tracerRadius = 0.05f;
            _tracerStartWidth = 0.1f;
            _tracerEndWidth = 0.05f;
            _tracerTailLength = 0.5f;
            _maxActiveTracers = 30;
            _tracerStartColor = new Color(1f, 0.9f, 0.2f, 0.9f);
            _tracerEndColor = new Color(1f, 0.6f, 0f, 0.3f);
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

        /// <summary>
        /// 运行时代码创建锁定激光线。本组件由 ProcedureBattle 以 AddComponent 挂载，
        /// 序列化 prefab 字段永远为 null，不能依赖 Inspector 配置。
        /// </summary>
        private void InitializeRocketLaser()
        {
            var go = new GameObject("RocketLockOnLaser");
            go.transform.SetParent(transform, false);

            _rocketLaser = go.AddComponent<LineRenderer>();
            _rocketLaser.positionCount = 2;
            _rocketLaser.startWidth = 0.05f;
            _rocketLaser.endWidth = 0.05f;
            _rocketLaser.material = _tracerMaterial;
            _rocketLaser.startColor = LaserIdleColor;
            _rocketLaser.endColor = LaserIdleColor;
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
                tracer.Transform.position = tracer.Position.ToWorld(BALLISTIC_HEIGHT);

                // 更新 tracer 拖尾线段
                if (tracer.LineRenderer != null)
                {
                    float tailLength = Mathf.Min(_tracerTailLength, Vector2.Distance(tracer.Position, tracer.HitPoint));
                    tracer.LineRenderer.SetPosition(0, tracer.Position.ToWorld(BALLISTIC_HEIGHT));
                    tracer.LineRenderer.SetPosition(1, (tracer.Position - tracer.Direction * tailLength).ToWorld(BALLISTIC_HEIGHT));
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

            // 弹道起点修正：WeaponSystem 传来的 origin 是角色中心（WeaponSystem 不感知视图层枪口），
            // 玩家开火时改从 WeaponMountView 枪口（武器模型前端）起算，与枪口火焰/瞄准激光同源。
            // 起点平移后必须让方向重新指向瞄准点，否则子弹不经过准星；
            // 上游辅助瞄准/扩散对角向的修正量（direction 与 rawDirection 的夹角）原样保留叠加。
            var player = PlayerSystem.Instance?.GetPlayerEntity();
            bool isPlayerFire = player != null && ownerId == ((IWeaponOwner)player).OwnerId;
            if (isPlayerFire)
            {
                Vector2 muzzle = GetMuzzleWorldPos(player).ToXZ();
                Vector2 aimPos = player.AimPosition;
                Vector2 toAim = aimPos - muzzle;
                if (toAim.sqrMagnitude > 1e-6f)
                {
                    Vector2 rawFromCenter = (aimPos - origin);
                    if (rawFromCenter.sqrMagnitude > 1e-6f)
                    {
                        float aimDelta = Vector2.SignedAngle(rawFromCenter.normalized, direction);
                        direction = Quaternion.Euler(0f, 0f, aimDelta) * toAim.normalized;
                    }
                    origin = muzzle;
                }

                // 枪口火焰只挂玩家武器（SpawnMuzzleFlash 内部取玩家枪口，队友开火不能闪在玩家枪口上）
                SpawnMuzzleFlash(direction);
            }

            if (config.ballisticType == BallisticType.Raycast)
            {
                FireRaycast(origin, direction, config, ownerId, isPlayerFire);
            }
            else if (config.ballisticType == BallisticType.Projectile)
            {
                FireProjectile(origin, direction, config, ownerId);
            }
        }

        /// <summary>
        /// 枪口火焰：从武器枪口（WeaponMountView 模型前端，复用火箭激光的缓存）沿射击方向播放。
        /// World 挂载——开火瞬间定死，移动中开火不拖尾。
        /// </summary>
        private void SpawnMuzzleFlash(Vector2 direction)
        {
            var player = PlayerSystem.Instance?.GetPlayerEntity();
            if (player == null) return;

            Vector3 muzzle = GetMuzzleWorldPos(player);
            var dir3 = new Vector3(direction.x, 0f, direction.y);
            if (dir3.sqrMagnitude < 1e-6f) return;

            GameEvent.Get<IEffectEvent>()?.OnPlayEffect(
                EffectIds.MuzzleFlash, muzzle, Quaternion.LookRotation(dir3), EffectContext.Default);
        }

        /// <summary>
        /// 命中特效：命中敌人播橙红火花（HitSpark），命中场景/障碍播灰白碎屑（HitSparkEnv）。
        /// </summary>
        private static void SpawnHitSpark(Vector3 hitPoint, GameObject hitTarget)
        {
            bool isEnemy = hitTarget != null && hitTarget.layer == LayerMask.NameToLayer("Enemy");
            var pos = hitPoint;
            pos.y = BALLISTIC_HEIGHT;
            GameEvent.Get<IEffectEvent>()?.OnPlayEffect(
                isEnemy ? EffectIds.HitSpark : EffectIds.HitSparkEnv, pos, Quaternion.identity, EffectContext.Default);
        }

        private void FireRaycast(Vector2 origin, Vector2 direction, WeaponConfig config, int ownerId, bool isPlayerFire)
        {
            float maxDistance = config.maxRange;
            float radius = config.raycastRadius >= 0 ? config.raycastRadius : _tracerRadius;
            LayerMask layers = config.hitLayers != 0 ? config.hitLayers : _hitLayers;

            // 3D 物理：玩法平面 (x, z) 转到世界坐标后在弹道高度上做 SphereCast
            Vector3 rayOrigin = origin.ToWorld(BALLISTIC_HEIGHT);
            Vector3 rayDirection = direction.ToWorld();
            bool hasHit = Physics.SphereCast(rayOrigin, radius, rayDirection, out RaycastHit hit, maxDistance, layers);

            Vector2 hitPoint = origin + direction * maxDistance;
            GameObject hitTarget = null;
            if (hasHit)
            {
                hitPoint = hit.point.ToXZ();
                hitTarget = hit.collider.gameObject;
                SpawnHitSpark(hit.point, hitTarget);

                // 立即伤害（命中反馈统一由 BattleSystem 触发，避免重复）
                // 技能树伤害加成只对玩家开火生效（队友/敌人不吃玩家技能加成）
                var damageInfo = MemoryPool.Acquire<DamageInfo>();
                damageInfo.AttackerId = ownerId;
                damageInfo.WeaponConfigId = config.id;
                damageInfo.TargetGameObject = hitTarget;
                damageInfo.Damage = isPlayerFire
                    ? Mathf.RoundToInt(config.damage * (1f + SkillSystem.GetEffect(GameConfig.cfg.ESkillEffect.DamagePct)))
                    : config.damage;
                damageInfo.HitDirection = (hitPoint - origin).normalized;
                damageInfo.HitPoint = hitPoint;
                GameEvent.Get<IBattleEvent>().OnEntityDamaged(damageInfo);
            }

            // Debug 可视化
            if (config.showDebugRay)
            {
                DrawDebugRaycast(origin, direction, hitPoint, maxDistance, hasHit, config);
            }

            // 延迟 tracer 视觉（开镜狙击直接命中镜窗中心，不播放子弹飞行动画；仅玩家狙击适用）
            if (!isPlayerFire || WeaponSystem.Instance == null || !WeaponSystem.Instance.IsScopedSniping)
            {
                SpawnTracer(origin, hitPoint, direction, config);
            }
        }

        private void DrawDebugRaycast(Vector2 origin, Vector2 direction, Vector2 hitPoint, float maxDistance, bool hasHit, WeaponConfig config)
        {
            Color color = hasHit ? config.debugHitColor : config.debugMissColor;
            Vector2 endPoint = hasHit ? hitPoint : origin + direction * maxDistance;

            // 主射线（XZ 玩法平面，绘制在弹道高度上）
            Debug.DrawLine(origin.ToWorld(BALLISTIC_HEIGHT), endPoint.ToWorld(BALLISTIC_HEIGHT), color, config.debugRayDuration);

            if (hasHit)
            {
                // 命中点十字标记（XZ 平面内）
                Vector3 hitWorld = hitPoint.ToWorld(BALLISTIC_HEIGHT);
                Debug.DrawRay(hitWorld, Vector3.forward * 0.2f, color, config.debugRayDuration);
                Debug.DrawRay(hitWorld, Vector3.back * 0.2f, color, config.debugRayDuration);
                Debug.DrawRay(hitWorld, Vector3.left * 0.2f, color, config.debugRayDuration);
                Debug.DrawRay(hitWorld, Vector3.right * 0.2f, color, config.debugRayDuration);
            }
        }

        private void FireProjectile(Vector2 origin, Vector2 direction, WeaponConfig config, int ownerId)
        {
            // RPG 为直线爆炸物：不做自动索敌，沿瞄准方向直射
            ProjectileSystem.Instance?.CreateProjectile(config.id, ownerId, origin, direction);
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
            tracer.Transform.position = origin.ToWorld(BALLISTIC_HEIGHT);

            if (tracer.LineRenderer != null)
            {
                tracer.LineRenderer.SetPosition(0, origin.ToWorld(BALLISTIC_HEIGHT));
                tracer.LineRenderer.SetPosition(1, (origin + direction * Mathf.Min(_tracerTailLength, distance)).ToWorld(BALLISTIC_HEIGHT));
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
            lr.startColor = _tracerStartColor;
            lr.endColor = _tracerEndColor;

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
            if (_rocketLaser == null) return;

            var player = PlayerSystem.Instance?.GetPlayerEntity();
            var weaponConfig = WeaponSystem.Instance?.CurrentWeapon?.Config;
            bool rocketEquipped = weaponConfig != null && weaponConfig.weaponType == WeaponType.Rocket;
            if (!rocketEquipped || player == null)
            {
                _rocketLaser.enabled = false;
                return;
            }

            // 激光常态展示（RPG 无锁定）：从武器枪口沿瞄准方向延伸的直线瞄准指示。
            _rocketLaser.enabled = true;
            Vector3 origin = GetMuzzleWorldPos(player);

            Vector2 aimOffset = player.AimPosition - player.transform.position.ToXZ();
            Vector2 dir = aimOffset.sqrMagnitude > 1e-6f ? aimOffset.normalized : Vector2.up;
            // 激光长度复用 maxRange（弹体最大射程），与火箭实际飞行距离一致
            float range = weaponConfig.maxRange > 0 ? weaponConfig.maxRange : 20f;
            _rocketLaser.SetPosition(0, origin);
            // 末端保持与枪口同高，激光呈水平直线
            _rocketLaser.SetPosition(1, origin + new Vector3(dir.x, 0f, dir.y) * range);
        }

        /// <summary>
        /// 取当前枪口世界坐标（WeaponMountView 模型前端）。
        /// 玩家实体不变时复用缓存的枪口组件，仅玩家切换/重建时懒获取一次。
        /// </summary>
        private Vector3 GetMuzzleWorldPos(PlayerEntity player)
        {
            if (_mountViewOwner != player)
            {
                _mountViewOwner = player;
                _cachedMountView = player.GetComponent<WeaponMountView>();
            }
            return _cachedMountView != null
                ? _cachedMountView.GetMuzzleWorldPos()
                : player.transform.position + Vector3.up * BALLISTIC_HEIGHT;
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
