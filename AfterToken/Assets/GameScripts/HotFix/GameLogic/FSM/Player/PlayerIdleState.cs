using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家待机状态。
    /// </summary>
    public class PlayerIdleState : PlayerStateBase
    {
        public override string StateName => "Idle";

        protected override void OnUpdate(IFsm<PlayerEntity> fsm, float elapse, float real)
        {
            base.OnUpdate(fsm, elapse, real);

            var owner = fsm.Owner;
            if (owner.MoveDirection.sqrMagnitude > 0.001f)
            {
                ChangeState<PlayerMoveState>(fsm);
            }
        }
    }
}
