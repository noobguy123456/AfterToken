using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 队友状态基类（与 EnemyStateBase 同模式：中断仲裁 + 延迟请求）。
    /// </summary>
    public abstract class CompanionStateBase : FsmState<CompanionEntity>
    {
        public abstract string StateName { get; }
        protected CompanionEntity Owner { get; private set; }
        protected CompanionStateContext Context => Owner?.Context;

        protected override void OnEnter(IFsm<CompanionEntity> fsm)
        {
            Owner = fsm.Owner;
            string prev = fsm.GetData<string>("PrevState") ?? "None";
            GameEvent.Get<ICompanionEvent>()?.OnCompanionStateChanged(StateName, prev);
            OnEnterState(fsm);
        }

        protected override void OnLeave(IFsm<CompanionEntity> fsm, bool isShutdown)
        {
            fsm.SetData("PrevState", StateName);
            OnLeaveState(fsm, isShutdown);
        }

        protected sealed override void OnUpdate(IFsm<CompanionEntity> fsm, float elapseSeconds, float realElapseSeconds)
        {
            Owner = fsm.Owner;

            // 中断仲裁：驱动器按优先级算出期望状态，与当前不同则切换
            var desired = CompanionStateMachineDriver.Instance.GetDesiredState(Context, GetType());
            if (desired != null && desired != GetType())
            {
                ChangeState(fsm, desired);
                return;
            }

            if (TryConsumePendingRequest(fsm)) return;
            OnUpdateState(fsm, elapseSeconds, realElapseSeconds);
        }

        protected void RequestState<T>(int priority = 0, object userData = null) where T : CompanionStateBase
        {
            if (Context == null) return;
            Context.PendingRequest = new StateTransitionRequest(typeof(T), priority, userData);
        }

        private bool TryConsumePendingRequest(IFsm<CompanionEntity> fsm)
        {
            var req = Context?.PendingRequest;
            if (req == null) return false;
            Context.PendingRequest = null;
            if (req.TargetStateType == GetType()) return false;
            ChangeState(fsm, req.TargetStateType);
            return true;
        }

        protected virtual void OnEnterState(IFsm<CompanionEntity> fsm) { }
        protected virtual void OnUpdateState(IFsm<CompanionEntity> fsm, float elapse, float real) { }
        protected virtual void OnLeaveState(IFsm<CompanionEntity> fsm, bool isShutdown) { }

        /// <summary>停下移动（有刚体则清速度）。</summary>
        protected void StopMoving()
        {
            if (Owner?.Rigidbody != null)
            {
                Owner.Rigidbody.linearVelocity = Vector3.zero;
            }
        }
    }
}
