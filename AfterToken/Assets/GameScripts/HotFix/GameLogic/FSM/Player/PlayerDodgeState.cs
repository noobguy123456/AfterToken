using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家翻滚/闪避状态。
    /// </summary>
    public class PlayerDodgeState : PlayerStateBase
    {
        public override string StateName => "Dodge";

        private float _elapsed;

        protected override void OnEnterState(IFsm<PlayerEntity> fsm)
        {
            _elapsed = 0f;
            Owner.StartDodge();
            Context.IsDodging = true;
            PlayerSystem.Instance?.ConsumeStamina(PlayerSystem.Instance.GetDodgeStaminaCost());
        }

        protected override void OnUpdateState(IFsm<PlayerEntity> fsm, float elapse, float real)
        {
            _elapsed += elapse;
            if (_elapsed >= Owner.DodgeDuration)
            {
                RequestState<PlayerIdleState>();
            }
        }

        protected override void OnLeaveState(IFsm<PlayerEntity> fsm, bool isShutdown)
        {
            Owner.EndDodge();
            Context.IsDodging = false;
        }
    }
}
