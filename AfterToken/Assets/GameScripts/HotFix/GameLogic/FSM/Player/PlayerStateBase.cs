using System;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家状态基类。
    /// 统一处理状态切换请求与全局拦截器，派生状态只需实现 OnUpdateState。
    /// </summary>
    public abstract class PlayerStateBase : FsmState<PlayerEntity>
    {
        public abstract string StateName { get; }

        /// <summary>
        /// 当前 FSM 持有者引用。
        /// </summary>
        protected PlayerEntity Owner { get; private set; }

        /// <summary>
        /// 玩家状态黑板。
        /// </summary>
        protected PlayerStateContext Context => Owner?.Context;

        protected override void OnEnter(IFsm<PlayerEntity> fsm)
        {
            Owner = fsm.Owner;
            Owner.PlayAnimation(StateName);

            string prev = fsm.GetData<string>("PrevState") ?? "None";
            GameEvent.Get<IPlayerEvent>().OnPlayerStateChanged(StateName, prev);

            OnEnterState(fsm);
        }

        protected override void OnLeave(IFsm<PlayerEntity> fsm, bool isShutdown)
        {
            fsm.SetData("PrevState", StateName);
            OnLeaveState(fsm, isShutdown);
        }

        /// <summary>
        /// 密封 Update：先处理全局拦截器，再消费状态自身请求，最后执行状态逻辑。
        /// </summary>
        protected sealed override void OnUpdate(IFsm<PlayerEntity> fsm, float elapseSeconds, float realElapseSeconds)
        {
            Owner = fsm.Owner;

            // 1. 全局拦截器（高优先级打断）
            if (PlayerStateMachineDriver.Instance.TryGetInterruptRequest(fsm.Owner.Context, GetType(), out var interruptRequest))
            {
                if (interruptRequest?.TargetStateType != null && interruptRequest.TargetStateType != GetType())
                {
                    ChangeState(fsm, interruptRequest.TargetStateType);
                    return;
                }
            }

            // 2. 消费状态自身发出的请求
            if (TryConsumePendingRequest(fsm))
                return;

            // 3. 执行状态自身逻辑
            OnUpdateState(fsm, elapseSeconds, realElapseSeconds);
        }

        /// <summary>
        /// 请求切换到指定状态。由 Driver 在本帧统一处理。
        /// </summary>
        protected void RequestState<T>(int priority = 0, object userData = null) where T : PlayerStateBase
        {
            if (Context == null) return;
            Context.PendingRequest = new StateTransitionRequest(typeof(T), priority, userData);
        }

        private bool TryConsumePendingRequest(IFsm<PlayerEntity> fsm)
        {
            if (Context == null) return false;

            var req = Context.PendingRequest;
            if (req == null) return false;

            Context.PendingRequest = null;

            if (req.TargetStateType == GetType())
                return false;

            ChangeState(fsm, req.TargetStateType);
            return true;
        }

        protected virtual void OnEnterState(IFsm<PlayerEntity> fsm) { }
        protected virtual void OnLeaveState(IFsm<PlayerEntity> fsm, bool isShutdown) { }
        protected virtual void OnUpdateState(IFsm<PlayerEntity> fsm, float elapseSeconds, float realElapseSeconds) { }
    }
}
