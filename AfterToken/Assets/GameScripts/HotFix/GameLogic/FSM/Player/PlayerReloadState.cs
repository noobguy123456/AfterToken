using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家换弹状态。
    /// 由武器实例自身管理换弹计时，状态机监听换弹完成/取消事件后切回 Idle。
    /// </summary>
    public class PlayerReloadState : PlayerStateBase
    {
        public override string StateName => "Reload";

        private IFsm<PlayerEntity> _fsm;

        protected override void OnEnterState(IFsm<PlayerEntity> fsm)
        {
            _fsm = fsm;

            GameEvent.AddEventListener<int, bool>(IWeaponEvent_Event.OnReloadStateChanged, OnReloadStateChanged);

            Context.IsReloading = true;
            Context.CurrentWeapon?.Reload(Owner.GetInstanceID());
        }

        protected override void OnUpdateState(IFsm<PlayerEntity> fsm, float elapse, float real)
        {
            // 武器实例负责计时，状态机只等待完成事件
        }

        protected override void OnLeaveState(IFsm<PlayerEntity> fsm, bool isShutdown)
        {
            GameEvent.RemoveEventListener<int, bool>(IWeaponEvent_Event.OnReloadStateChanged, OnReloadStateChanged);
            _fsm = null;
            Context.IsReloading = false;
        }

        /// <summary>
        /// 换弹完成或被取消时切回 Idle。
        /// </summary>
        private void OnReloadStateChanged(int ownerId, bool isReloading)
        {
            if (isReloading) return;
            if (_fsm != null && _fsm.IsRunning && _fsm.CurrentState == this)
            {
                RequestState<PlayerIdleState>();
            }
        }
    }
}
