using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家换弹状态。
    /// </summary>
    public class PlayerReloadState : PlayerStateBase
    {
        public override string StateName => "Reload";

        private IFsm<PlayerEntity> _fsm;

        protected override void OnEnter(IFsm<PlayerEntity> fsm)
        {
            base.OnEnter(fsm);

            _fsm = fsm;
            GameEvent.AddEventListener<int, int>(IWeaponEvent_Event.OnWeaponSwitched, OnWeaponSwitched);

            var owner = fsm.Owner;
            GameEvent.Get<IWeaponEvent>().OnReload(owner.GetInstanceID());

            // 从当前武器配置读取换弹时间
            float reloadTime = WeaponSystem.Instance?.CurrentWeapon?.Config.reloadTime ?? 1.5f;

            int timerId = GameModule.Timer.AddTimer(
                (args) =>
                {
                    if (fsm.IsRunning)
                    {
                        ChangeState<PlayerIdleState>(fsm);
                    }
                },
                time: reloadTime
            );

            fsm.SetData("ReloadTimerId", timerId);
        }

        protected override void OnLeave(IFsm<PlayerEntity> fsm, bool isShutdown)
        {
            base.OnLeave(fsm, isShutdown);

            GameEvent.RemoveEventListener<int, int>(IWeaponEvent_Event.OnWeaponSwitched, OnWeaponSwitched);
            _fsm = null;

            int timerId = fsm.GetData<int>("ReloadTimerId");
            if (timerId != 0)
            {
                GameModule.Timer.RemoveTimer(timerId);
                fsm.SetData("ReloadTimerId", 0);
            }
        }

        /// <summary>
        /// 换弹过程中切换武器时，立即中断换弹并回到 Idle。
        /// 武器的 CancelReload 由 WeaponSystem.SwitchToSlot 负责调用。
        /// </summary>
        private void OnWeaponSwitched(int ownerId, int slot)
        {
            if (_fsm != null && _fsm.IsRunning && _fsm.CurrentState == this)
            {
                ChangeState<PlayerIdleState>(_fsm);
            }
        }
    }
}
