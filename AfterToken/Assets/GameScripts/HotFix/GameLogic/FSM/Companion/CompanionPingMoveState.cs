using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 队友前往标点状态（Move/Loot 标点）：寻路到标点，到位后清除标点并转入驻守。
    /// M5 起复用为 LLM 移动类指令（move_to/loot/extract）的执行体：无玩家标点时读 LLM 目标。
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
            bool fromPing = Context.HasPing;
            Vector2 targetPos;
            if (fromPing)
            {
                targetPos = Context.PingPos;
            }
            else if (Context.HasLlmMoveDirective)
            {
                targetPos = Context.LlmTargetPos;
            }
            else
            {
                // 标点被外部清除（过期/覆盖）且无 LLM 指令：回姿态，由驱动器下帧仲裁
                return;
            }

            bool arrived = _mover.MoveTowards(Owner, Context.NavigationSystem, targetPos, elapse, ARRIVE_DISTANCE);
            if (arrived)
            {
                // 到位：消费指令，驻守在此（玩家可按 G 或新标点召回）
                Context.StanceHold = true;
                if (fromPing)
                {
                    PingSystem.Instance?.ClearPing();
                }
                else
                {
                    Context.ClearLlmDirective();
                }
            }
        }

        protected override void OnLeaveState(IFsm<CompanionEntity> fsm, bool isShutdown)
        {
            _mover.Stop(Owner);
        }
    }
}
