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
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Rigidbody2D _rb;

        [Header("血条")]
        [SerializeField] private Transform _healthBarRoot;
        [SerializeField] private SpriteRenderer _healthBarBackground;
        [SerializeField] private SpriteRenderer _healthBarFill;

        private const float HEALTH_BAR_WIDTH = 1.0f;
        private const float HEALTH_BAR_HEIGHT = 0.12f;
        private const float HEALTH_BAR_FILL_HEIGHT = 0.08f;
        private const float HEALTH_BAR_OFFSET_Y = 0.6f;

        private IFsm<EnemyEntity> _fsm;

        public int ConfigId => _configId;
        public int Hp => _hp;
        public int MaxHp => _maxHp;
        public bool IsDead => _hp <= 0;

        /// <summary>
        /// 敌人状态机黑板。
        /// </summary>
        public EnemyStateContext Context { get; private set; }

        /// <summary>
        /// 敌人刚体。
        /// </summary>
        public Rigidbody2D Rigidbody => _rb;

        private void Awake()
        {
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            EnsureRigidbody();
        }

        /// <summary>
        /// 确保刚体配置与玩家一致：Dynamic、无重力、冻结旋转。
        /// </summary>
        private void EnsureRigidbody()
        {
            if (_rb == null)
            {
                _rb = gameObject.AddComponent<Rigidbody2D>();
            }
            _rb.gravityScale = 0;
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        public void Initialize(int configId, int maxHp)
        {
            _configId = configId;
            _maxHp = maxHp;
            _hp = maxHp;

            Context = new EnemyStateContext();
            Context.IsDead = false;

            EnsureHealthBar();
            UpdateHealthBar();
            CreateFsm();
        }

        public void TakeDamage(int damage, Vector2 hitDirection)
        {
            if (IsDead) return;

            _hp -= damage;
            if (_hp < 0) _hp = 0;

            UpdateHealthBar();

            if (_hp <= 0)
            {
                Context.IsDead = true;
            }
        }

        /// <summary>
        /// 根据状态名播放动画。
        /// </summary>
        public void PlayAnimation(string stateName)
        {
            // TODO: 接入 TbEnemyAnimation 配置表
            string animName = stateName switch
            {
                "Idle" => "Enemy_Idle",
                "Chase" => "Enemy_Run",
                "Attack" => "Enemy_Attack",
                "Dead" => "Enemy_Dead",
                _ => null
            };

            if (string.IsNullOrEmpty(animName))
            {
                Log.Warning($"[EnemyEntity] 找不到状态 {stateName} 对应的动画");
                return;
            }

            if (_animator != null)
            {
                _animator.Play(animName, 0, 0f);
            }
        }

        /// <summary>
        /// 设置朝向。
        /// </summary>
        public void SetFacing(Vector2 direction)
        {
            if (direction.x > 0.01f)
            {
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
            else if (direction.x < -0.01f)
            {
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
        }

        private void CreateFsm()
        {
            if (_fsm != null)
            {
                GameModule.Fsm.DestroyFsm(_fsm);
                _fsm = null;
            }

            _fsm = GameModule.Fsm.CreateFsm<EnemyEntity>(
                $"EnemyFsm_{GetInstanceID()}",
                this,
                new EnemyIdleState(),
                new EnemyChaseState(),
                new EnemyAttackState(),
                new EnemyDeadState()
            );

            _fsm.Start<EnemyIdleState>();
        }

        private void Update()
        {
            if (Context == null) return;
            EnemyStateMachineDriver.Instance.UpdateContext(Context, this);
        }

        private void OnDestroy()
        {
            if (_fsm != null)
            {
                GameModule.Fsm.DestroyFsm(_fsm);
                _fsm = null;
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
    }
}
