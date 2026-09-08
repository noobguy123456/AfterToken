using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 队友驻守状态：原地警戒（跟随开关关闭或标点到位后进入）。
    /// </summary>
    public class CompanionHoldState : CompanionStateBase
    {
        public override string StateName => "Hold";

        protected override void OnEnterState(IFsm<CompanionEntity> fsm)
        {
            StopMoving();
        }
    }
}
