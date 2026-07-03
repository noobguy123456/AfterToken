using System;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 敌人状态基类。
    /// </summary>
    public abstract class EnemyStateBase : FsmState<EnemyEntity>
    {
        public abstract string StateName { get; }
        protected EnemyEntity Owner { get; private set; }
        protected EnemyStateContext Context => Owner?.Context;

        protected override void OnEnter(IFsm<EnemyEntity> fsm)
        {
            Owner = fsm.Owner;
            Owner.PlayAnimation(StateName);
            string prev = fsm.GetData<string>("PrevState") ?? "None";
            GameEvent.Get<IEnemyEvent>().OnEnemyStateChanged(Owner.GetInstanceID(), StateName, prev);
            OnEnterState(fsm);
        }

        protected override void OnLeave(IFsm<EnemyEntity> fsm, bool isShutdown)
        {
            fsm.SetData("PrevState", StateName);
            OnLeaveState(fsm, isShutdown);
        }

        protected sealed override void OnUpdate(IFsm<EnemyEntity> fsm, float elapseSeconds, float realElapseSeconds)
        {
            Owner = fsm.Owner;
            if (EnemyStateMachineDriver.Instance.TryGetInterruptRequest(Context, GetType(), out var request))
            {
                if (request?.TargetStateType != null && request.TargetStateType != GetType())
                {
                    ChangeState(fsm, request.TargetStateType);
                    return;
                }
            }
            if (TryConsumePendingRequest(fsm)) return;
            OnUpdateState(fsm, elapseSeconds, realElapseSeconds);
        }

        protected void RequestState<T>(int priority = 0, object userData = null) where T : EnemyStateBase
        {
            if (Context == null) return;
            Context.PendingRequest = new StateTransitionRequest(typeof(T), priority, userData);
        }

        private bool TryConsumePendingRequest(IFsm<EnemyEntity> fsm)
        {
            if (Context == null) return false;
            var req = Context.PendingRequest;
            if (req == null) return false;
            Context.PendingRequest = null;
            if (req.TargetStateType == GetType()) return false;
            ChangeState(fsm, req.TargetStateType);
            return true;
        }

        protected virtual void OnEnterState(IFsm<EnemyEntity> fsm) { }
        protected virtual void OnLeaveState(IFsm<EnemyEntity> fsm, bool isShutdown) { }
        protected virtual void OnUpdateState(IFsm<EnemyEntity> fsm, float elapseSeconds, float realElapseSeconds) { }
    }
}
