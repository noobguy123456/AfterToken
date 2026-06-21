using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家移动状态。
    /// </summary>
    public class PlayerMoveState : PlayerStateBase
    {
        public override string StateName => "Move";

        protected override void OnUpdate(IFsm<PlayerEntity> fsm, float elapse, float real)
        {
            var owner = fsm.Owner;

            if (owner.MoveDirection.sqrMagnitude <= 0.001f)
            {
                ChangeState<PlayerIdleState>(fsm);
            }
        }
    }
}
