using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 队友前往标点状态（Move/Loot 标点）：寻路到标点，到位后清除标点并转入驻守。
    /// </summary>
    public class CompanionPingMoveState : CompanionStateBase
    {
        public override string StateName => "PingMove";

        private const float ARRIVE_DISTANCE = 0.6f;

        private readonly CompanionPathMover _mover = new CompanionPathMover();

        protected override void OnEnterState(IFsm<CompanionEntity> fsm)
        {
            _mover.Reset();
        }

        protected override void OnUpdateState(IFsm<CompanionEntity> fsm, float elapse, float real)
        {
            if (!Context.HasPing)
            {
                // 标点被外部清除（过期/覆盖）：回姿态，由驱动器下帧仲裁
                return;
            }

            bool arrived = _mover.MoveTowards(Owner, Context.NavigationSystem, Context.PingPos, elapse, ARRIVE_DISTANCE);
            if (arrived)
            {
                // 到位：消费标点，驻守在此（玩家可按 G 或新标点召回）
                Context.StanceHold = true;
                PingSystem.Instance?.ClearPing();
            }
        }

        protected override void OnLeaveState(IFsm<CompanionEntity> fsm, bool isShutdown)
        {
            _mover.Stop(Owner);
        }
    }
}
