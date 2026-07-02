using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家死亡状态。
    /// </summary>
    public class PlayerDeadState : PlayerStateBase
    {
        public override string StateName => "Dead";

        protected override void OnEnterState(IFsm<PlayerEntity> fsm)
        {
            Owner.SetDead();
            GameEvent.Get<IPlayerEvent>().OnPlayerDied();
        }
    }
}
