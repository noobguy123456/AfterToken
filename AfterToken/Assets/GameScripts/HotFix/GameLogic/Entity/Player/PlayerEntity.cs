using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家实体。
    /// 负责玩家表现、动画、物理移动。
    /// 玩法平面为世界 XZ 地面（y=0），逻辑坐标 Vector2 语义为 (x, z)。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerEntity : MonoBehaviour, IDamageable, IWeaponOwner
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        // 以下四项运行时状态的唯一数据源是黑板 PlayerStateContext（见下方 Context 属性），
        // 本实体不再持有副本，属性全部转发到黑板；黑板未赋值（创建流程中）时读取回退默认值、写入丢弃，
        // 与原来的字段初始值行为一致。

        /// <summary>
        /// 移动输入方向（转发自黑板 <see cref="PlayerStateContext.MoveInput"/>）。
        /// </summary>
        public Vector2 MoveDirection
        {
            get => Context?.MoveInput ?? Vector2.zero;
            private set { if (Context != null) Context.MoveInput = value; }
        }

        /// <summary>
        /// 瞄准位置（转发自黑板 <see cref="PlayerStateContext.AimInput"/>）。
        /// </summary>
        public Vector2 AimPosition
        {
            get => Context?.AimInput ?? Vector2.zero;
            private set { if (Context != null) Context.AimInput = value; }
        }

        public float BaseMoveSpeed { get; set; }
        public float MoveSpeed { get; set; }
        public float DodgeSpeed { get; set; }
        public float DodgeDuration { get; set; }
        public bool IsMoving => MoveDirection.sqrMagnitude > 0.001f;

        /// <summary>
        /// 是否死亡（转发自黑板 <see cref="PlayerStateContext.IsDead"/>）。
        /// </summary>
        public bool IsDead
        {
            get => Context != null && Context.IsDead;
            private set { if (Context != null) Context.IsDead = value; }
        }

        /// <summary>
        /// 是否正在闪避（转发自黑板 <see cref="PlayerStateContext.IsDodging"/>）。
        /// </summary>
        public bool IsDodging
        {
            get => Context != null && Context.IsDodging;
            private set { if (Context != null) Context.IsDodging = value; }
        }

        #region IWeaponOwner

        int IWeaponOwner.OwnerId => GetInstanceID();
        Vector2 IWeaponOwner.Position => transform.position.ToXZ();
        Vector2 IWeaponOwner.AimPosition => AimPosition;
        Vector2 IWeaponOwner.MoveDirection => MoveDirection;
        bool IWeaponOwner.IsMoving => IsMoving;

        #endregion

        /// <summary>
        /// 玩家状态机黑板。
        /// </summary>
        public PlayerStateContext Context { get; set; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void SetMoveDirection(Vector2 dir) => MoveDirection = dir;
        public void SetAimPosition(Vector2 pos) => AimPosition = pos;

        /// <summary>
        /// 根据状态名播放动画。
        /// </summary>
        public void PlayAnimation(string stateName)
        {
            var playerConfig = ConfigSystem.Instance?.Tables?.TbPlayer?.GetOrDefault(1);
            string animName = stateName switch
            {
                "Idle" => playerConfig?.IdleAnim ?? "Player_Idle",
                "Move" => playerConfig?.MoveAnim ?? "Player_Run",
                "Dodge" => playerConfig?.DodgeAnim ?? "Player_Roll",
                "Reload" => playerConfig?.ReloadAnim ?? "Player_Reload",
                "Dead" => playerConfig?.DeadAnim ?? "Player_Dead",
                _ => null
            };

            if (string.IsNullOrEmpty(animName))
            {
                Log.Warning($"[PlayerEntity] 找不到状态 {stateName} 对应的动画");
                return;
            }

            if (_animator != null)
            {
                _animator.Play(animName, 0, 0f);
            }
        }

        public void StartDodge()
        {
            IsDodging = true;
            _rb.linearVelocity = new Vector3(MoveDirection.x, 0f, MoveDirection.y).normalized * DodgeSpeed;
        }

        public void EndDodge()
        {
            IsDodging = false;
        }

        public void SetDead()
        {
            IsDead = true;
            _rb.linearVelocity = Vector3.zero;
            _rb.isKinematic = true;
        }

        public void ResetEntity()
        {
            IsDead = false;
            IsDodging = false;
            _rb.isKinematic = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.linearVelocity = Vector3.zero;
            MoveDirection = Vector2.zero;
            MoveSpeed = BaseMoveSpeed;
        }

        public bool TakeDamage(int damage, Vector2 hitDirection)
        {
            if (IsDead) return false;
            GameEvent.Get<IPlayerEvent>().OnPlayerDamaged(damage, hitDirection);
            return true;
        }

        private void Update()
        {
            if (IsDead) return;

            // 朝向瞄准位置（渲染帧更新，保证视觉流畅）；XZ 平面上绕 Y 轴旋转
            Vector2 aimDir = (AimPosition - transform.position.ToXZ()).normalized;
            if (aimDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(new Vector3(aimDir.x, 0f, aimDir.y));
            }
        }

        private void FixedUpdate()
        {
            if (IsDead) return;

            // 闪避期间不覆盖速度；闪避由 StartDodge 设置，EndDodge 结束
            if (!IsDodging)
            {
                _rb.linearVelocity = new Vector3(MoveDirection.x, 0f, MoveDirection.y) * MoveSpeed;
            }

            // 边界钳制：防止走出战斗地面（XZ 平面）
            // 注意：Rigidbody.position 赋值等于传送，会打断插值导致移动卡顿，
            // 因此只有确实越界（钳制值与当前值不同）时才回写。
            if (BattleBoundary.HasBounds)
            {
                Vector3 clamped = BattleBoundary.Clamp(_rb.position);
                if (clamped != _rb.position)
                {
                    _rb.position = clamped;
                }
            }

            // 广播位置变化（供非视觉系统使用，相机已直接读取 Transform）
            GameEvent.Get<IPlayerEvent>().OnPlayerPositionChanged(transform.position);
        }
    }
}
