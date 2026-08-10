using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 敌人实体。
    /// </summary>
    public class EnemyEntity : MonoBehaviour, IDamageable
    {
        [SerializeField] private int _configId;
        [SerializeField] private int _maxHp;
        [SerializeField] private int _hp;
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Rigidbody _rb;

        [Header("血条")]
        [SerializeField] private Transform _healthBarRoot;
        [SerializeField] private SpriteRenderer _healthBarBackground;
        [SerializeField] private SpriteRenderer _healthBarFill;

        private const float HEALTH_BAR_WIDTH = 1.0f;
        private const float HEALTH_BAR_HEIGHT = 0.12f;
        private const float HEALTH_BAR_FILL_HEIGHT = 0.08f;
        private const float HEALTH_BAR_OFFSET_Y = 0.6f;

        // 血条的固定世界偏移（EnsureHealthBar 时捕获）：敌人刚体不锁 Y 旋转，
        // 物理推挤会让根节点打转，血条挂在根节点下会跟着转，因此每帧钉住。
        private Vector3 _healthBarFixedOffset;
        private Quaternion _healthBarFixedRotation;

        // 血条朝向基准相机（屏幕对齐 billboard 用），静态缓存避免每个敌人每帧 Find
        private static Camera _healthBarCamera;

        // 已迁移到 PlaceholderSpriteProvider.GetWhiteSprite4()

        private IFsm<EnemyEntity> _fsm;

        public int ConfigId => _configId;
        public int Hp => _hp;
        public int MaxHp => _maxHp;
        public bool IsDead => _hp <= 0;
        public float MoveSpeed { get; private set; }
        public int AttackDamage { get; private set; }
        public float AttackRange { get; private set; }
        public float AttackInterval { get; private set; }
        public float PathRefreshInterval { get; private set; }

        /// <summary>
        /// 仇恨范围（追击触发距离），由 TbEnemy 配置注入。
        /// </summary>
        public float ChaseRange { get; private set; }

        /// <summary>
        /// 对象池标识。死亡回收时用于归还到对应池中。
        /// </summary>
        public string PoolKey { get; set; }

        /// <summary>
        /// 敌人状态机黑板。
        /// </summary>
        public EnemyStateContext Context { get; private set; }

        /// <summary>
        /// 敌人刚体。
        /// </summary>
        public Rigidbody Rigidbody => _rb;

        private void Awake()
        {
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_spriteRenderer == null)
            {
                // 角色贴图已移到 Visual 子节点（X+90° 平躺渲染），优先从该节点取，避免误取到血条 SpriteRenderer
                var visual = transform.Find("Visual");
                _spriteRenderer = visual != null ? visual.GetComponent<SpriteRenderer>() : GetComponent<SpriteRenderer>();
            }
            if (_rb == null) _rb = GetComponent<Rigidbody>();
            EnsureRigidbody();
        }

        private void OnEnable()
        {
            EnemyRegistry.Register(this);
        }

        private void OnDisable()
        {
            EnemyRegistry.Unregister(this);
        }

        /// <summary>
        /// 确保刚体配置与玩家一致：无重力、锁死 Y 位置、仅保留绕 Y 旋转。
        /// </summary>
        private void EnsureRigidbody()
        {
            if (_rb == null)
            {
                _rb = gameObject.AddComponent<Rigidbody>();
            }
            _rb.useGravity = false;
            _rb.isKinematic = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        /// <summary>
        /// 重置物理状态，用于对象池复用时恢复碰撞体和刚体。
        /// </summary>
        private void ResetPhysics()
        {
            EnsureRigidbody();
            _rb.linearVelocity = Vector3.zero;
            _rb.isKinematic = false;

            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = true;
            }
        }

        /// <summary>
        /// 回收敌人对象到对象池；未配置池时直接销毁。
        /// </summary>
        public void Recycle()
        {
            if (!string.IsNullOrEmpty(PoolKey) && PoolSystem.Instance != null)
            {
                PoolSystem.Instance.Recycle(PoolKey, gameObject);
            }
            else
            {
                Object.Destroy(gameObject);
            }
        }

        public void Initialize(int configId, int maxHp, float moveSpeed, int attackDamage, float attackRange, float attackInterval, float pathRefreshInterval = 0.3f, float chaseRange = 5f)
        {
            _configId = configId;
            _maxHp = maxHp;
            _hp = maxHp;
            MoveSpeed = moveSpeed;
            AttackDamage = attackDamage;
            AttackRange = attackRange;
            AttackInterval = attackInterval;
            PathRefreshInterval = pathRefreshInterval;
            ChaseRange = chaseRange;

            Context = new EnemyStateContext();
            Context.IsDead = false;

            ResetPhysics();
            EnsureHealthBar();
            UpdateHealthBar();
            CreateFsm();
        }

        public bool TakeDamage(int damage, Vector2 hitDirection)
        {
            if (IsDead) return false;

            _hp -= damage;
            if (_hp < 0) _hp = 0;

            UpdateHealthBar();

            if (_hp <= 0)
            {
                Context.IsDead = true;
            }

            return true;
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
        /// 设置朝向。只翻转身体 Sprite，不翻转整个 Transform，避免血条等子对象晃动。
        /// </summary>
        public void SetFacing(Vector2 direction)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.flipX = direction.x < -0.01f;
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
            EnemyRegistry.Unregister(this);
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

            var whiteSprite = PlaceholderSpriteProvider.GetWhiteSprite4();

            // 确保 Prefab 中已有的 SpriteRenderer 也有 sprite，防止美术未指定时无法显示。
            if (_healthBarBackground != null && _healthBarBackground.sprite == null)
            {
                _healthBarBackground.sprite = whiteSprite;
            }
            if (_healthBarFill != null && _healthBarFill.sprite == null)
            {
                _healthBarFill.sprite = whiteSprite;
            }

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

            // 捕获生成时刻的世界朝向与偏移，作为血条的固定基准
            _healthBarFixedRotation = _healthBarRoot.rotation;
            _healthBarFixedOffset = _healthBarRoot.position - transform.position;
        }

        /// <summary>
        /// 每帧钉住血条：位置固定在敌人头顶偏移处，朝向与主相机保持一致（屏幕对齐 billboard），
        /// 敌人被物理推挤打转、相机偏航/俯仰变化时，血条在屏幕上的角度都保持不变。
        /// </summary>
        private void LateUpdate()
        {
            if (_healthBarRoot == null) return;

            if (_healthBarCamera == null)
            {
                _healthBarCamera = Camera.main;
            }

            // 血条平面平行屏幕（X=屏幕右、Y=屏幕上）；无相机时退回生成时刻的固定朝向
            _healthBarRoot.rotation = _healthBarCamera != null
                ? _healthBarCamera.transform.rotation
                : _healthBarFixedRotation;
            _healthBarRoot.position = transform.position + _healthBarFixedOffset;
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


    }
}
