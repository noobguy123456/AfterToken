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
        [SerializeField] private int _enemyCount = 3;
        [SerializeField] private float _spawnRadius = 6f;
        [SerializeField] private int _enemyConfigId = 9001;
        [SerializeField] private int _enemyMaxHp = 50;

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
            SpawnEnemiesAsync().Forget();
        }

        private async UniTask SpawnEnemiesAsync()
        {
            for (int i = 0; i < _enemyCount; i++)
            {
                float angle = i * 120f * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * _spawnRadius;

                var go = await GameModule.Resource.LoadGameObjectAsync("Enemy", transform);
                if (go == null)
                {
                    go = CreatePlaceholderEnemy(pos);
                }
                else
                {
                    go.transform.position = pos;
                }

                var enemy = go.GetComponent<EnemyEntity>();
                if (enemy == null) enemy = go.AddComponent<EnemyEntity>();
                enemy.Initialize(_enemyConfigId, _enemyMaxHp);

                GameEvent.Get<IEnemyEvent>().OnEnemySpawned(enemy.GetInstanceID(), _enemyConfigId);

                await UniTask.Yield();
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
            sr.sprite = CreatePlaceholderSprite();
            sr.color = Color.red;
            sr.sortingOrder = 5;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.3f;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.bodyType = RigidbodyType2D.Kinematic;

            return go;
        }

        private Sprite CreatePlaceholderSprite()
        {
            var tex = new Texture2D(16, 16);
            for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
                tex.SetPixel(x, y, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        }
    }
}
