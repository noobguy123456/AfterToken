using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 队友跟随状态（默认姿态）：保持与玩家 followStopDist（读 TbCompanion）以内，超出则寻路追赶。
    /// </summary>
    public class CompanionFollowState : CompanionStateBase
    {
        public override string StateName => "Follow";

        /// <summary>到位距离兜底值（TbCompanion.followStopDist 缺失时用）。</summary>
        private const float FOLLOW_STOP_DISTANCE_FALLBACK = 2.2f;

        private readonly CompanionPathMover _mover = new CompanionPathMover();
        private float _stopDistance = FOLLOW_STOP_DISTANCE_FALLBACK;

        protected override void OnEnterState(IFsm<CompanionEntity> fsm)
        {
            _mover.Reset();
            var persona = CompanionConfigMgr.Instance.Get();
            if (persona != null && persona.FollowStopDist > 0.01f)
            {
                _stopDistance = persona.FollowStopDist;
            }
        }

        protected override void OnUpdateState(IFsm<CompanionEntity> fsm, float elapse, float real)
        {
            if (!Context.PlayerExists)
            {
                StopMoving();
                return;
            }

            bool arrived = _mover.MoveTowards(Owner, Context.NavigationSystem, Context.PlayerPosition, elapse, _stopDistance);
            if (arrived)
            {
                // 到位后面朝玩家移动方向的朝向保持不变即可（不强制转身）
            }
        }

        protected override void OnLeaveState(IFsm<CompanionEntity> fsm, bool isShutdown)
        {
            _mover.Stop(Owner);
        }
    }
}
