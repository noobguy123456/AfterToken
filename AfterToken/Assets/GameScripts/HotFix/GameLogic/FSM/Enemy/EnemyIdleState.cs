using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 敌人待机状态。
    /// </summary>
    public class EnemyIdleState : EnemyStateBase
    {
        public override string StateName => "Idle";

        protected override void OnEnterState(IFsm<EnemyEntity> fsm)
        {
            if (Owner.Rigidbody != null)
            {
                Owner.Rigidbody.linearVelocity = Vector2.zero;
            }
        }

        protected override void OnUpdateState(IFsm<EnemyEntity> fsm, float elapse, float real)
        {
            if (Context.WantsToAttack)
            {
                RequestState<EnemyAttackState>();
            }
            else if (Context.WantsToChase)
            {
                RequestState<EnemyChaseState>();
            }
        }
    }
}
