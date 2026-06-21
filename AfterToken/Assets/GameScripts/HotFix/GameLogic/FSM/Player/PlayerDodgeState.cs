using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家翻滚状态。
    /// </summary>
    public class PlayerDodgeState : PlayerStateBase
    {
        public override string StateName => "Dodge";

        protected override void OnEnter(IFsm<PlayerEntity> fsm)
        {
            base.OnEnter(fsm);

            var owner = fsm.Owner;
            owner.StartDodge();

            int timerId = GameModule.Timer.AddTimer(
                (args) =>
                {
                    if (fsm.IsRunning)
                    {
                        ChangeState<PlayerIdleState>(fsm);
                    }
                },
                time: owner.DodgeDuration
            );

            fsm.SetData("DodgeTimerId", timerId);
        }

        protected override void OnLeave(IFsm<PlayerEntity> fsm, bool isShutdown)
        {
            base.OnLeave(fsm, isShutdown);

            var owner = fsm.Owner;
            owner.EndDodge();

            int timerId = fsm.GetData<int>("DodgeTimerId");
            if (timerId != 0)
            {
                GameModule.Timer.RemoveTimer(timerId);
                fsm.SetData("DodgeTimerId", 0);
            }
        }
    }
}
