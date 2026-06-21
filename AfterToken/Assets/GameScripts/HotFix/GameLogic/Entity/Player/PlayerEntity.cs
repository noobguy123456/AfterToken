using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家实体。
    /// 负责玩家表现、动画、物理移动。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerEntity : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        public Vector2 MoveDirection { get; private set; }
        public Vector2 AimPosition { get; private set; }
        public float BaseMoveSpeed { get; set; } = 5f;
        public float MoveSpeed { get; set; } = 5f;
        public float DodgeSpeed { get; set; } = 15f;
        public float DodgeDuration { get; set; } = 0.4f;
        public bool IsMoving => MoveDirection.sqrMagnitude > 0.001f;
        public bool IsDead { get; private set; }
        public bool IsDodging { get; private set; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
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
            // TODO: 接入 TbPlayerAnimation 配置表
            string animName = stateName switch
            {
                "Idle" => "Player_Idle",
                "Move" => "Player_Run",
                "Dodge" => "Player_Roll",
                "Reload" => "Player_Reload",
                "Dead" => "Player_Dead",
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
            _rb.linearVelocity = MoveDirection.normalized * DodgeSpeed;
        }

        public void EndDodge()
        {
            IsDodging = false;
        }

        public void SetDead()
        {
            IsDead = true;
            _rb.linearVelocity = Vector2.zero;
            _rb.bodyType = RigidbodyType2D.Kinematic;
        }

        public void ResetEntity()
        {
            IsDead = false;
            IsDodging = false;
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.linearVelocity = Vector2.zero;
            MoveDirection = Vector2.zero;
            MoveSpeed = BaseMoveSpeed;
        }

        private void FixedUpdate()
        {
            if (IsDead) return;

            // 闪避期间不覆盖速度；闪避由 StartDodge 设置，EndDodge 结束
            if (!IsDodging)
            {
                _rb.linearVelocity = MoveDirection * MoveSpeed;
            }

            // 朝向瞄准位置
            Vector2 aimDir = (AimPosition - (Vector2)transform.position).normalized;
            if (aimDir.sqrMagnitude > 0.001f)
            {
                transform.up = aimDir;
            }

            // 广播位置变化
            GameEvent.Get<IPlayerEvent>().OnPlayerPositionChanged(transform.position);
        }
    }
}
