using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家待机状态。
    /// </summary>
    public class PlayerIdleState : PlayerStateBase
    {
        public override string StateName => "Idle";

        protected override void OnUpdateState(IFsm<PlayerEntity> fsm, float elapse, float real)
        {
            if (Context.MoveInput.sqrMagnitude > 0.001f)
            {
                RequestState<PlayerMoveState>();
            }
        }
    }
}
