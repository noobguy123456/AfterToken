using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 敌人攻击状态。
    /// </summary>
    public class EnemyAttackState : EnemyStateBase
    {
        public override string StateName => "Attack";

        private float _elapsed;
        private float _attackDuration = 0.5f;

        protected override void OnEnterState(IFsm<EnemyEntity> fsm)
        {
            _elapsed = 0f;
            if (Owner.Rigidbody != null)
            {
                Owner.Rigidbody.linearVelocity = Vector2.zero;
            }
            // TODO: 触发近战/远程攻击逻辑
        }

        protected override void OnUpdateState(IFsm<EnemyEntity> fsm, float elapse, float real)
        {
            _elapsed += elapse;
            if (_elapsed >= _attackDuration)
            {
                RequestState<EnemyIdleState>();
            }
        }
    }
}
