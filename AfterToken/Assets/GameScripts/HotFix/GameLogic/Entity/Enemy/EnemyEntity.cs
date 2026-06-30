using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 敌人实体。
    /// </summary>
    public class EnemyEntity : MonoBehaviour
    {
        [SerializeField] private int _configId;
        [SerializeField] private int _maxHp = 50;
        [SerializeField] private int _hp = 50;

        [Header("血条")]
        [SerializeField] private Transform _healthBarRoot;
        [SerializeField] private SpriteRenderer _healthBarBackground;
        [SerializeField] private SpriteRenderer _healthBarFill;

        private const float HEALTH_BAR_WIDTH = 1.0f;
        private const float HEALTH_BAR_HEIGHT = 0.12f;
        private const float HEALTH_BAR_FILL_HEIGHT = 0.08f;
        private const float HEALTH_BAR_OFFSET_Y = 0.6f;

        public int ConfigId => _configId;
        public int Hp => _hp;
        public int MaxHp => _maxHp;
        public bool IsDead => _hp <= 0;

        public void Initialize(int configId, int maxHp)
        {
            _configId = configId;
            _maxHp = maxHp;
            _hp = maxHp;

            EnsureHealthBar();
            UpdateHealthBar();
        }

        public void TakeDamage(int damage, Vector2 hitDirection)
        {
            if (IsDead) return;

            _hp -= damage;
            if (_hp < 0) _hp = 0;

            UpdateHealthBar();

            if (_hp <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// 确保血条已创建。
        /// 优先使用 Prefab 中已配置的血条节点；未配置则运行时动态创建占位血条。
        /// </summary>
        private void EnsureHealthBar()
        {
            if (_healthBarRoot == null)
            {
                _healthBarRoot = transform.Find("HealthBarRoot");
            }

            if (_healthBarRoot != null)
            {
                if (_healthBarBackground == null)
                {
                    var bgTrans = _healthBarRoot.Find("Background");
                    if (bgTrans != null) _healthBarBackground = bgTrans.GetComponent<SpriteRenderer>();
                }

                if (_healthBarFill == null)
                {
                    var fillTrans = _healthBarRoot.Find("Fill");
                    if (fillTrans != null) _healthBarFill = fillTrans.GetComponent<SpriteRenderer>();
                }
            }
            else
            {
                var rootGo = new GameObject("HealthBarRoot");
                rootGo.transform.SetParent(transform, false);
                rootGo.transform.localPosition = new Vector3(0f, HEALTH_BAR_OFFSET_Y, 0f);
                _healthBarRoot = rootGo.transform;
            }

            var whiteSprite = CreateWhiteSprite();

            if (_healthBarBackground == null)
            {
                var bgGo = new GameObject("Background");
                bgGo.transform.SetParent(_healthBarRoot, false);
                _healthBarBackground = bgGo.AddComponent<SpriteRenderer>();
                _healthBarBackground.sprite = whiteSprite;
                _healthBarBackground.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                _healthBarBackground.sortingOrder = 10;
                _healthBarBackground.transform.localScale = new Vector3(HEALTH_BAR_WIDTH, HEALTH_BAR_HEIGHT, 1f);
            }

            if (_healthBarFill == null)
            {
                var fillGo = new GameObject("Fill");
                fillGo.transform.SetParent(_healthBarRoot, false);
                _healthBarFill = fillGo.AddComponent<SpriteRenderer>();
                _healthBarFill.sprite = whiteSprite;
                _healthBarFill.sortingOrder = 11;
                _healthBarFill.transform.localScale = new Vector3(HEALTH_BAR_WIDTH, HEALTH_BAR_FILL_HEIGHT, 1f);
            }
        }

        private void UpdateHealthBar()
        {
            if (_healthBarFill == null) return;

            float ratio = _maxHp > 0 ? (float)_hp / _maxHp : 0f;
            _healthBarFill.transform.localScale = new Vector3(ratio * HEALTH_BAR_WIDTH, HEALTH_BAR_FILL_HEIGHT, 1f);

            if (ratio > 0.6f) _healthBarFill.color = Color.green;
            else if (ratio > 0.3f) _healthBarFill.color = Color.yellow;
            else _healthBarFill.color = Color.red;
        }

        private Sprite CreateWhiteSprite()
        {
            var tex = new Texture2D(4, 4);
            for (int x = 0; x < 4; x++)
            for (int y = 0; y < 4; y++)
                tex.SetPixel(x, y, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0f, 0.5f), 4);
        }

        private void Die()
        {
            GameEvent.Get<IEnemyEvent>().OnEnemyDied(GetInstanceID());
            Destroy(gameObject);
        }
    }
}
