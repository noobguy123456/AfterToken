using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 队友撤退状态（低血）：向玩家收缩至 2.5m 内，威胁清空后由驱动器切回姿态。
    /// MVP 敌人不索敌队友，该状态为受伤链路就绪后的兜底行为。
    /// </summary>
    public class CompanionRetreatState : CompanionStateBase
    {
        public override string StateName => "Retreat";

        private const float RETREAT_STOP_DISTANCE = 2.5f;

        private readonly CompanionPathMover _mover = new CompanionPathMover();

        protected override void OnEnterState(IFsm<CompanionEntity> fsm)
        {
            _mover.Reset();
        }

        protected override void OnUpdateState(IFsm<CompanionEntity> fsm, float elapse, float real)
        {
            if (!Context.PlayerExists)
            {
                StopMoving();
                return;
            }

            _mover.MoveTowards(Owner, Context.NavigationSystem, Context.PlayerPosition, elapse, RETREAT_STOP_DISTANCE);
        }

        protected override void OnLeaveState(IFsm<CompanionEntity> fsm, bool isShutdown)
        {
            _mover.Stop(Owner);
        }
    }
}
