using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家换弹状态。
    /// </summary>
    public class PlayerReloadState : PlayerStateBase
    {
        public override string StateName => "Reload";

        protected override void OnEnter(IFsm<PlayerEntity> fsm)
        {
            base.OnEnter(fsm);

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

            int timerId = fsm.GetData<int>("ReloadTimerId");
            if (timerId != 0)
            {
                GameModule.Timer.RemoveTimer(timerId);
                fsm.SetData("ReloadTimerId", 0);
            }
        }
    }
}
