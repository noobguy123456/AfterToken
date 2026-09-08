using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// AI 队友实体（占位视觉：胶囊 + 右侧武器盒，正式模型接入后替换 CreateVisual）。
    /// 行为完全由 FSM/Companion 驱动；本实体只承载数值、受伤、血条与朝向。
    /// MVP 敌人索敌不变（只追玩家），队友受伤链路已就绪但暂无伤害来源。
    /// </summary>
    public class CompanionEntity : MonoBehaviour, IDamageable
    {
        /// <summary>队友呼号（字幕显示名，人设卡接入后由 TbCompanion 提供）。</summary>
        public const string CompanionName = "Rook";

        private const float HEALTH_BAR_WIDTH = 1.0f;
        private const float HEALTH_BAR_HEIGHT = 0.12f;
        private const float HEALTH_BAR_FILL_HEIGHT = 0.08f;
        private const float HEALTH_BAR_OFFSET_Y = 1.35f;

        /// <summary>队友武器默认值（TbCompanion.weaponConfigId 缺失时的兜底）。</summary>
        public const int DefaultWeaponConfigId = 1003;

        [SerializeField] private int _maxHp = 200;
        [SerializeField] private int _hp = 200;

        private Transform _visual;
        private Renderer _visualRenderer;
        private Transform _healthBarRoot;
        private SpriteRenderer _healthBarFill;
        private Vector3 _healthBarFixedOffset;
        private Quaternion _healthBarFixedRotation;
        private static Camera _healthBarCamera;

        private Rigidbody _rb;
        private IFsm<CompanionEntity> _fsm;

        public int Hp => _hp;
        public int MaxHp => _maxHp;
        public bool IsDead => _hp <= 0;
        public float MoveSpeed { get; private set; } = 4.2f;
        public int OwnerId => GetInstanceID();

        /// <summary>持有武器配置 ID（TbCompanion.weaponConfigId，初始化时注入）。</summary>
        public int WeaponConfigId { get; private set; } = DefaultWeaponConfigId;

        /// <summary>
        /// 队友状态机黑板。
        /// </summary>
        public CompanionStateContext Context { get; private set; }

        public Rigidbody Rigidbody => _rb;

        /// <summary>
        /// 初始化并生成占位视觉（胶囊 + 武器盒 + 血条），启动 FSM。
        /// </summary>
        public void Initialize(int maxHp, float moveSpeed, int weaponConfigId = DefaultWeaponConfigId)
        {
            _maxHp = Mathf.Max(1, maxHp);
            _hp = _maxHp;
            MoveSpeed = moveSpeed > 0.01f ? moveSpeed : 4.2f;
            WeaponConfigId = weaponConfigId > 0 ? weaponConfigId : DefaultWeaponConfigId;

            Context = new CompanionStateContext();
            gameObject.layer = LayerMask.NameToLayer("Player") is int l && l >= 0 ? l : gameObject.layer;

            EnsureRigidbody();
            CreateVisual();
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

            if (_hp <= 0 && Context != null)
            {
                Context.IsDead = true;
            }
            return true;
        }

        /// <summary>
        /// 代码驱动朝向（刚体锁全部旋转，不受物理推挤影响）。
        /// </summary>
        public void SetFacing(Vector2 direction)
        {
            if (direction.sqrMagnitude < 1e-6f) return;
            transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.y));
        }

        /// <summary>
        /// 枪口世界坐标（武器盒前端，近似挂点；正式模型的 Muzzle socket 接入点）。
        /// </summary>
        public Vector3 GetMuzzleWorldPos()
        {
            return transform.position + Vector3.up * 0.7f + transform.forward * 0.55f + transform.right * 0.3f;
        }

        private void EnsureRigidbody()
        {
            if (_rb == null)
            {
                _rb = gameObject.AddComponent<Rigidbody>();
            }
            _rb.useGravity = false;
            _rb.isKinematic = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            // 高阻尼：被玩家/爆炸等物理推挤时冲量快速衰减，避免"碰一下就滑飞很远"
            // （移动由 linearVelocity 每帧直接赋值，不受阻尼影响）
            _rb.linearDamping = 8f;
            // 锁全部旋转：朝向由 SetFacing 代码驱动，物理推挤不转身（血条/武器盒不晃）
            _rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        }

        private void CreateVisual()
        {
            if (_visual != null) return;

            // 胶囊（与 NPC 同尺度：0.6 直径、1.2 高），颜色与玩家（青）/敌人（红）区分
            var visualGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visualGo.name = "Visual";
            visualGo.transform.SetParent(transform, false);
            visualGo.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            visualGo.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            visualGo.layer = gameObject.layer;
            _visual = visualGo.transform;
            _visualRenderer = visualGo.GetComponent<Renderer>();
            if (_visualRenderer != null)
            {
                _visualRenderer.material.color = new Color(0.45f, 0.85f, 0.50f);
            }

            // 右侧武器盒（步枪尺寸近似），纯表现不参与碰撞
            var weaponGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weaponGo.name = "WeaponModel";
            var col = weaponGo.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }
            weaponGo.layer = gameObject.layer;
            weaponGo.transform.SetParent(transform, false);
            weaponGo.transform.localPosition = new Vector3(0.3f, 0.7f, 0.28f);
            weaponGo.transform.localRotation = Quaternion.identity;
            weaponGo.transform.localScale = new Vector3(0.11f, 0.20f, 0.70f);
            weaponGo.GetComponent<MeshRenderer>().material.color = new Color(0.20f, 0.35f, 0.22f);
        }

        private void CreateFsm()
        {
            if (_fsm != null)
            {
                GameModule.Fsm.DestroyFsm(_fsm);
                _fsm = null;
            }

            _fsm = GameModule.Fsm.CreateFsm<CompanionEntity>(
                $"CompanionFsm_{GetInstanceID()}",
                this,
                new CompanionFollowState(),
                new CompanionHoldState(),
                new CompanionPingMoveState(),
                new CompanionEngageState(),
                new CompanionRetreatState(),
                new CompanionDeadState()
            );

            _fsm.Start<CompanionFollowState>();
        }

        private void Update()
        {
            if (Context == null) return;
            CompanionStateMachineDriver.Instance.UpdateContext(Context, this);
        }

        private void OnDestroy()
        {
            if (_fsm != null)
            {
                GameModule.Fsm.DestroyFsm(_fsm);
                _fsm = null;
            }
        }

        #region 血条（与 EnemyEntity 同套占位方案：钉头顶 + 屏幕对齐 billboard）

        private void EnsureHealthBar()
        {
            var rootGo = new GameObject("HealthBarRoot");
            rootGo.transform.SetParent(transform, false);
            rootGo.transform.localPosition = new Vector3(0f, HEALTH_BAR_OFFSET_Y, 0f);
            _healthBarRoot = rootGo.transform;

            var whiteSprite = PlaceholderSpriteProvider.GetWhiteSprite4();

            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(_healthBarRoot, false);
            // 白色 Sprite 枢轴在左边缘（(0,0.5)），左移半宽让整条血条居中于头顶
            bgGo.transform.localPosition = new Vector3(-HEALTH_BAR_WIDTH / 2f, 0f, 0f);
            var bg = bgGo.AddComponent<SpriteRenderer>();
            bg.sprite = whiteSprite;
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            bg.sortingOrder = 10;
            bg.transform.localScale = new Vector3(HEALTH_BAR_WIDTH, HEALTH_BAR_HEIGHT, 1f);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(_healthBarRoot, false);
            fillGo.transform.localPosition = new Vector3(-HEALTH_BAR_WIDTH / 2f, 0f, 0f);
            _healthBarFill = fillGo.AddComponent<SpriteRenderer>();
            _healthBarFill.sprite = whiteSprite;
            _healthBarFill.sortingOrder = 11;
            _healthBarFill.transform.localScale = new Vector3(HEALTH_BAR_WIDTH, HEALTH_BAR_FILL_HEIGHT, 1f);

            _healthBarFixedRotation = _healthBarRoot.rotation;
            _healthBarFixedOffset = _healthBarRoot.position - transform.position;
        }

        private void LateUpdate()
        {
            if (_healthBarRoot == null) return;

            if (_healthBarCamera == null)
            {
                _healthBarCamera = Camera.main;
            }

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

            // 队友血条固定绿色系（与敌人红黄绿渐变区分）
            _healthBarFill.color = ratio > 0.3f ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.9f, 0.6f, 0.1f);
        }

        #endregion
    }
}
