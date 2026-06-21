using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家状态基类。
    /// </summary>
    public abstract class PlayerStateBase : FsmState<PlayerEntity>
    {
        public abstract string StateName { get; }

        protected override void OnEnter(IFsm<PlayerEntity> fsm)
        {
            var owner = fsm.Owner;
            owner.PlayAnimation(StateName);
            string prev = fsm.GetData<string>("PrevState") ?? "None";
            GameEvent.Get<IPlayerEvent>().OnPlayerStateChanged(StateName, prev);
        }

        protected override void OnLeave(IFsm<PlayerEntity> fsm, bool isShutdown)
        {
            fsm.SetData("PrevState", StateName);
        }

        protected override void OnUpdate(IFsm<PlayerEntity> fsm, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);

            string nextState = fsm.GetData<string>("NextState");
            if (string.IsNullOrEmpty(nextState)) return;

            fsm.SetData<string>("NextState", null);

            switch (nextState)
            {
                case "Dodge":
                    ChangeState<PlayerDodgeState>(fsm);
                    break;
                case "Reload":
                    ChangeState<PlayerReloadState>(fsm);
                    break;
                case "Dead":
                    ChangeState<PlayerDeadState>(fsm);
                    break;
            }
        }
    }
}
