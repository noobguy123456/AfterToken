using TEngine;
using UnityEngine;
using GameLogic.Navigation;

namespace GameLogic
{
    /// <summary>
    /// 敌人散步状态：丢失玩家后的低速游走。
    /// 以进入时的位置为锚点，在 4m 半径内随机选可走点慢速移动，到达后停 1~2s 再选下一点。
    /// 不做攻击（攻击/重新追击由 AttackInterceptor/ChaseInterceptor 触发）。
    /// </summary>
    public class EnemyWanderState : EnemyStateBase
    {
        public override string StateName => "Wander";

        // 散步移速为追击移速的 0.4 倍
        private float MoveSpeed => (Owner?.MoveSpeed > 0.01f ? Owner.MoveSpeed : 2f) * 0.4f;

        private const float WANDER_RADIUS = 4f; // 以锚点为中心的游走半径
        private const float MIN_WANDER_DISTANCE = 1f; // 选点最小距离，避免原地抖动
        private const float ARRIVE_THRESHOLD = 0.2f;
        private const float MOVE_TIMEOUT = 3f; // 走不到（被障碍挡住）就换点

        private Vector2 _anchor;
        private Vector2 _target;
        private bool _hasTarget;
        private float _waitTimer;
        private float _moveTimer;

        protected override void OnEnterState(IFsm<EnemyEntity> fsm)
        {
            _anchor = Owner.transform.position.ToXZ();
            _hasTarget = false;
            _waitTimer = 0f;
            _moveTimer = 0f;
            StopMoving();
        }

        protected override void OnUpdateState(IFsm<EnemyEntity> fsm, float elapse, float real)
        {
            // 到达后的停留阶段
            if (_waitTimer > 0f)
            {
                _waitTimer -= elapse;
                StopMoving();
                return;
            }

            if (!_hasTarget)
            {
                PickNextTarget();
            }

            Vector2 ownerPos = Owner.transform.position.ToXZ();
            Vector2 toTarget = _target - ownerPos;
            if (toTarget.magnitude <= ARRIVE_THRESHOLD)
            {
                // 到达：停 1~2s 再选下一点
                _hasTarget = false;
                _waitTimer = Random.Range(1f, 2f);
                StopMoving();
                return;
            }

            _moveTimer += elapse;
            if (_moveTimer >= MOVE_TIMEOUT)
            {
                // 超时走不到，直接换点
                _hasTarget = false;
                return;
            }

            MoveTowards(toTarget.normalized);
        }

        protected override void OnLeaveState(IFsm<EnemyEntity> fsm, bool isShutdown)
        {
            StopMoving();
        }

        /// <summary>
        /// 在锚点 4m 半径内随机选点，并用导航吸附到最近可走格中心，保证不会选进障碍。
        /// </summary>
        private void PickNextTarget()
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(MIN_WANDER_DISTANCE, WANDER_RADIUS);
            Vector2 candidate = _anchor + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

            var nav = Context?.NavigationSystem;
            if (nav != null && nav.TryGetNearestWalkable(candidate, out var walkablePos))
            {
                candidate = walkablePos;
            }

            _target = candidate;
            _hasTarget = true;
            _moveTimer = 0f;
        }

        private void MoveTowards(Vector2 direction)
        {
            Owner.SetFacing(direction);
            if (Owner.Rigidbody != null)
            {
                Owner.Rigidbody.linearVelocity = new Vector3(direction.x, 0f, direction.y) * MoveSpeed;
            }
        }

        private void StopMoving()
        {
            if (Owner.Rigidbody != null)
            {
                Owner.Rigidbody.linearVelocity = Vector3.zero;
            }
        }
    }
}
