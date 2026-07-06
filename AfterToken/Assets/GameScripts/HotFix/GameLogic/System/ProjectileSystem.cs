using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TEngine;
using Cysharp.Threading.Tasks;

namespace GameLogic
{
    /// <summary>
    /// 子弹系统。
    /// 负责实体飞行物的生成、飞行、碰撞、回收与范围伤害。
    /// </summary>
    public class ProjectileSystem : MonoBehaviour
    {
        public static ProjectileSystem Instance { get; private set; }

        [SerializeField] private Transform _projectileRoot;

        private readonly Dictionary<int, ProjectileData> _activeProjectiles = new Dictionary<int, ProjectileData>();
        private readonly Dictionary<int, ProjectileEntity> _entityMap = new Dictionary<int, ProjectileEntity>();
        private readonly Queue<(ProjectileData data, RaycastHit2D hit)> _pendingHits = new Queue<(ProjectileData data, RaycastHit2D hit)>();
        private int _nextProjectileId = 1;
        private GameEventMgr _eventMgr = new GameEventMgr();

        private readonly Queue<GameObject> _projectilePool = new Queue<GameObject>();
        private readonly List<GameObject> _allProjectiles = new List<GameObject>();
        private readonly Dictionary<int, EnemyEntity> _enemyMap = new Dictionary<int, EnemyEntity>();
        private bool _preloaded;

        private void Awake()
        {
            Instance = this;

            if (_projectileRoot == null)
            {
                var root = new GameObject("ProjectileRoot");
                root.transform.SetParent(transform, false);
                _projectileRoot = root.transform;
            }

            _eventMgr.AddEvent<int, GameObject>(IProjectileEvent_Event.OnProjectileHit, OnProjectileHit);
            _eventMgr.AddEvent<int, int>(IEnemyEvent_Event.OnEnemySpawned, OnEnemySpawned);
            _eventMgr.AddEvent<int>(IEnemyEvent_Event.OnEnemyDied, OnEnemyDied);
        }

        private void OnDestroy()
        {
            _eventMgr.Clear();
            Instance = null;

            foreach (var go in _allProjectiles)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }

            _allProjectiles.Clear();
            _projectilePool.Clear();
        }

        private async UniTaskVoid Start()
        {
            await PreloadProjectilesAsync(5);
        }

        private async UniTask PreloadProjectilesAsync(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = await TryLoadProjectilePrefab();
                if (go != null)
                {
                    go.SetActive(false);
                    _projectilePool.Enqueue(go);
                    _allProjectiles.Add(go);
                }
            }

            _preloaded = true;
        }

        private async UniTask<GameObject> TryLoadProjectilePrefab()
        {
            var go = await GameModule.Resource.LoadGameObjectAsync("Projectile_Normal", _projectileRoot);
            if (go == null)
            {
                // 占位：动态创建一个简单飞行物
                go = CreatePlaceholderProjectile();
            }
            return go;
        }

        private GameObject CreatePlaceholderProjectile()
        {
            var go = new GameObject("Projectile_Placeholder");
            go.layer = LayerMask.NameToLayer("Projectile");
            go.transform.SetParent(_projectileRoot, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderSpriteProvider.GetWhiteSprite16();
            sr.color = Color.yellow;
            sr.sortingOrder = 10;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.1f;
            col.isTrigger = true;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            go.AddComponent<ProjectileEntity>();
            return go;
        }

        private void OnEnemySpawned(int enemyId, int configId)
        {
            // 延迟一帧查找，确保 EnemyEntity 已初始化
            FindEnemyAndCache(enemyId).Forget();
        }

        private async UniTaskVoid FindEnemyAndCache(int enemyId)
        {
            await UniTask.Yield();
            var enemy = GameObject.FindObjectsByType<EnemyEntity>(FindObjectsSortMode.None)
                .FirstOrDefault(e => e.GetInstanceID() == enemyId);
            if (enemy != null)
            {
                _enemyMap[enemyId] = enemy;
            }
        }

        private void OnEnemyDied(int enemyId)
        {
            _enemyMap.Remove(enemyId);
        }

        public void CreateProjectile(int weaponConfigId, int ownerId, Vector2 position, Vector2 direction, int targetId = 0, bool tracking = false)
        {
            var weaponConfig = WeaponConfigMgr.Instance?.Get(weaponConfigId);
            if (weaponConfig == null) return;

            var data = MemoryPool.Acquire<ProjectileData>();
            data.Id = _nextProjectileId++;
            data.ConfigId = weaponConfigId;
            data.OwnerId = ownerId;
            data.Position = position;
            data.Direction = direction.normalized;
            data.Speed = weaponConfig.projectileSpeed;
            data.LifeTime = weaponConfig.projectileLifeTime;
            data.Damage = weaponConfig.damage;
            data.PenetrateCount = 0;
            data.BounceCount = 0;
            data.LayerMask = LayerMask.GetMask("Enemy", "Obstacle");
            data.Radius = 0.2f;
            data.IsActive = true;
            data.IsTracking = tracking;
            data.TargetId = targetId;

            var entity = GetOrCreateEntity();
            if (entity == null)
            {
                MemoryPool.Release(data);
                return;
            }

            entity.Init(data);

            _activeProjectiles[data.Id] = data;
            _entityMap[data.Id] = entity;

            GameEvent.Get<IProjectileEvent>().OnProjectileCreated(data.Id, position);
        }

        private ProjectileEntity GetOrCreateEntity()
        {
            GameObject go = null;

            if (_projectilePool.Count > 0)
            {
                go = _projectilePool.Dequeue();
                go.SetActive(true);
            }
            else if (_preloaded)
            {
                // 池已空，动态创建一个占位飞行物
                go = CreatePlaceholderProjectile();
                _allProjectiles.Add(go);
                go.SetActive(true);
            }
            else
            {
                Log.Warning("[ProjectileSystem] 子弹池尚未预加载完成");
                return null;
            }

            var entity = go.GetComponent<ProjectileEntity>();
            if (entity == null) entity = go.AddComponent<ProjectileEntity>();
            return entity;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            // 使用字典直接遍历，避免每帧分配 List<int>
            var enumerator = _activeProjectiles.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var data = enumerator.Current.Value;
                if (!data.IsActive) continue;
                Tick(data, deltaTime);
            }
            enumerator.Dispose();

            ProcessPendingHits();
        }

        private void Tick(ProjectileData data, float deltaTime)
        {
            data.LifeTime -= deltaTime;
            if (data.LifeTime <= 0)
            {
                DestroyProjectile(data);
                return;
            }

            // 追踪逻辑
            if (data.IsTracking && data.TargetId != 0)
            {
                var target = FindTarget(data.TargetId);
                if (target != null)
                {
                    Vector2 toTarget = ((Vector2)target.transform.position - data.Position).normalized;
                    data.Direction = Vector2.Lerp(data.Direction, toTarget, 5f * deltaTime).normalized;
                }
            }

            float moveDistance = data.Speed * deltaTime;
            Vector2 newPosition = data.Position + data.Direction * moveDistance;

            RaycastHit2D hit = Physics2D.CircleCast(
                data.Position,
                data.Radius,
                data.Direction,
                moveDistance,
                data.LayerMask
            );

            if (hit.collider != null)
            {
                data.Position = hit.point;
                if (_entityMap.TryGetValue(data.Id, out var entity))
                {
                    entity.UpdateVisual();
                }
                _pendingHits.Enqueue((data, hit));
                return;
            }

            data.Position = newPosition;

        }

        private Transform FindTarget(int targetId)
        {
            if (_enemyMap.TryGetValue(targetId, out var enemy) && enemy != null)
            {
                return enemy.transform;
            }
            return null;
        }

        private void ProcessPendingHits()
        {
            while (_pendingHits.Count > 0)
            {
                var (data, hit) = _pendingHits.Dequeue();
                if (!_activeProjectiles.ContainsKey(data.Id)) continue;

                HandleHit(data, hit);
            }
        }

        private void HandleHit(ProjectileData data, RaycastHit2D hit)
        {
            var weaponConfig = WeaponConfigMgr.Instance?.Get(data.ConfigId);
            if (weaponConfig != null && weaponConfig.explosionRadius > 0)
            {
                ApplyExplosionDamage(data, hit.point, weaponConfig);
            }
            else
            {
                var damageInfo = MemoryPool.Acquire<DamageInfo>();
                damageInfo.AttackerId = data.OwnerId;
                damageInfo.WeaponConfigId = data.ConfigId;
                damageInfo.BulletConfigId = data.ConfigId;
                damageInfo.Damage = data.Damage;
                damageInfo.TargetGameObject = hit.collider != null ? hit.collider.gameObject : null;
                damageInfo.HitDirection = data.Direction;
                damageInfo.HitPoint = data.Position;

                GameEvent.Get<IBattleEvent>().OnEntityDamaged(damageInfo);
            }

            if (data.PenetrateCount > 0)
            {
                data.PenetrateCount--;
            }
            else
            {
                DestroyProjectile(data);
            }
        }

        private void ApplyExplosionDamage(ProjectileData data, Vector2 center, WeaponConfig weaponConfig)
        {
            float radius = weaponConfig.explosionRadius;
            var hits = Physics2D.OverlapCircleAll(center, radius, LayerMask.GetMask("Enemy"));

            foreach (var col in hits)
            {
                float distance = Vector2.Distance(center, col.transform.position);
                float falloff = Mathf.Clamp01(1 - distance / radius);
                float damage = data.Damage * (1 - weaponConfig.explosionDamageFalloff * (1 - falloff));

                var damageInfo = MemoryPool.Acquire<DamageInfo>();
                damageInfo.AttackerId = data.OwnerId;
                damageInfo.WeaponConfigId = data.ConfigId;
                damageInfo.BulletConfigId = data.ConfigId;
                damageInfo.Damage = damage;
                damageInfo.TargetGameObject = col.gameObject;

                GameEvent.Get<IBattleEvent>().OnEntityDamaged(damageInfo);
            }

            // 爆炸特效占位
            // SpawnExplosionEffect(center, weaponConfig);
        }

        private void OnProjectileHit(int projectileId, GameObject target)
        {
            if (!_activeProjectiles.TryGetValue(projectileId, out var data)) return;

            var weaponConfig = WeaponConfigMgr.Instance?.Get(data.ConfigId);
            if (weaponConfig != null && weaponConfig.explosionRadius > 0)
            {
                ApplyExplosionDamage(data, data.Position, weaponConfig);
            }
            else
            {
                var damageInfo = MemoryPool.Acquire<DamageInfo>();
                damageInfo.AttackerId = data.OwnerId;
                damageInfo.WeaponConfigId = data.ConfigId;
                damageInfo.TargetGameObject = target;
                damageInfo.BulletConfigId = data.ConfigId;
                damageInfo.Damage = data.Damage;
                damageInfo.HitDirection = data.Direction;
                damageInfo.HitPoint = data.Position;

                GameEvent.Get<IBattleEvent>().OnEntityDamaged(damageInfo);
            }

            if (data.PenetrateCount <= 0)
            {
                DestroyProjectile(data);
            }
            else
            {
                data.PenetrateCount--;
            }
        }

        public void DestroyProjectile(ProjectileData data)
        {
            if (!_activeProjectiles.ContainsKey(data.Id)) return;

            data.IsActive = false;
            _activeProjectiles.Remove(data.Id);

            if (_entityMap.TryGetValue(data.Id, out var entity))
            {
                _entityMap.Remove(data.Id);
                entity.OnRecycle();
                RecycleProjectile(entity.gameObject);
            }

            MemoryPool.Release(data);

            GameEvent.Get<IProjectileEvent>().OnProjectileDestroyed(data.Id);
        }

        private void RecycleProjectile(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            _projectilePool.Enqueue(go);
        }
    }
}
