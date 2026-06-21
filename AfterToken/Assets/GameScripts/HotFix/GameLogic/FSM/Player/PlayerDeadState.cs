using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家死亡状态。
    /// </summary>
    public class PlayerDeadState : PlayerStateBase
    {
        public override string StateName => "Dead";

        protected override void OnEnter(IFsm<PlayerEntity> fsm)
        {
            base.OnEnter(fsm);

            var owner = fsm.Owner;
            owner.SetDead();
            GameEvent.Get<IPlayerEvent>().OnPlayerDied();
        }
    }
}
