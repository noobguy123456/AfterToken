using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 敌人生成系统（占位实现）。
    /// </summary>
    public class EnemySpawnSystem : MonoBehaviour
    {
        [SerializeField] private int _enemyCount;
        [SerializeField] private float _spawnRadius;
        [SerializeField] private int _enemyConfigId;
        [SerializeField] private int _enemyMaxHp;

        /// <summary>
        /// 初始化敌人生成配置（需在 Start 前调用）。
        /// </summary>
        public void Initialize(int count, float radius, int configId, int maxHp)
        {
            _enemyCount = count;
            _spawnRadius = radius;
            _enemyConfigId = configId;
            _enemyMaxHp = maxHp;
        }

        private void Start()
        {
            SpawnEnemiesAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask SpawnEnemiesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Vector2 spawnCenter = PlayerSystem.Instance?.SpawnPosition ?? Vector2.zero;
            var enemyCfg = LoadEnemyConfig(_enemyConfigId);
            if (enemyCfg == null)
            {
                Log.Warning($"[EnemySpawnSystem] 找不到敌人配置 {_enemyConfigId}，使用默认值");
            }

            string prefabAddress = !string.IsNullOrEmpty(enemyCfg?.Prefab) ? enemyCfg.Prefab : "Enemy";
            int maxHp = _enemyMaxHp > 0 ? _enemyMaxHp : (enemyCfg?.MaxHp ?? 50);
            float moveSpeed = enemyCfg?.MoveSpeed ?? 2f;
            int attackDamage = enemyCfg?.AttackDamage ?? 5;
            float attackRange = enemyCfg?.AttackRange ?? 1.2f;
            float attackInterval = enemyCfg?.AttackInterval ?? 0.5f;
            float pathRefreshInterval = enemyCfg?.PathRefreshInterval ?? 0.3f;

            GameObject prefab = await LoadEnemyPrefabAsync(prefabAddress, cancellationToken);
            string poolKey = !string.IsNullOrEmpty(prefabAddress) ? prefabAddress : "Enemy_Placeholder";

            if (PoolSystem.Instance != null)
            {
                PoolSystem.Instance.Preload(poolKey, prefab, transform, _enemyCount);
                prefab.SetActive(false); // 隐藏作为模板的预制体本身
            }

            for (int i = 0; i < _enemyCount; i++)
            {
                float angle = i * (360f / _enemyCount) * Mathf.Deg2Rad;
                Vector2 spawnPos = spawnCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _spawnRadius;

                GameObject go;
                if (PoolSystem.Instance != null)
                {
                    go = PoolSystem.Instance.Get(poolKey, prefab, transform);
                }
                else
                {
                    go = Object.Instantiate(prefab, transform);
                }
                go.transform.position = spawnPos;

                var enemy = go.GetComponent<EnemyEntity>();
                if (enemy == null) enemy = go.AddComponent<EnemyEntity>();
                enemy.Initialize(_enemyConfigId, maxHp, moveSpeed, attackDamage, attackRange, attackInterval, pathRefreshInterval);
                enemy.PoolKey = poolKey;

                GameEvent.Get<IEnemyEvent>().OnEnemySpawned(enemy.GetInstanceID(), _enemyConfigId);

                await UniTask.Yield(cancellationToken);
            }
        }

        private async UniTask<GameObject> LoadEnemyPrefabAsync(string prefabAddress, CancellationToken cancellationToken)
        {
            var go = await GameModule.Resource.LoadGameObjectAsync(prefabAddress, transform, cancellationToken);
            if (go == null && prefabAddress != "Enemy")
            {
                Log.Warning($"[EnemySpawnSystem] 加载敌人预制体 {prefabAddress} 失败，回退到 Enemy");
                go = await GameModule.Resource.LoadGameObjectAsync("Enemy", transform, cancellationToken);
            }
            if (go == null)
            {
                go = CreatePlaceholderEnemy(Vector2.zero);
                go.SetActive(false);
                go.name = "Enemy_Placeholder_Prefab";
            }
            return go;
        }

        private GameConfig.cfg.Enemy LoadEnemyConfig(int configId)
        {
            try
            {
                return ConfigSystem.Instance?.Tables?.TbEnemy?.GetOrDefault(configId);
            }
            catch (System.Exception e)
            {
                Log.Warning($"[EnemySpawnSystem] 读取敌人配置 {configId} 失败: {e.Message}");
                return null;
            }
        }

        private GameObject CreatePlaceholderEnemy(Vector3 position)
        {
            var go = new GameObject("Enemy_Placeholder");
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.tag = "Enemy";
            go.layer = LayerMask.NameToLayer("Enemy");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderSpriteProvider.GetWhiteSprite16();
            sr.color = Color.red;
            sr.sortingOrder = 5;
            go.transform.localScale = Vector3.one * 0.3f;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.15f;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            return go;
        }


    }
}
