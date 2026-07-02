using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家移动状态。
    /// </summary>
    public class PlayerMoveState : PlayerStateBase
    {
        public override string StateName => "Move";

        protected override void OnUpdateState(IFsm<PlayerEntity> fsm, float elapse, float real)
        {
            if (Context.MoveInput.sqrMagnitude <= 0.001f)
            {
                RequestState<PlayerIdleState>();
            }
        }
    }
}
