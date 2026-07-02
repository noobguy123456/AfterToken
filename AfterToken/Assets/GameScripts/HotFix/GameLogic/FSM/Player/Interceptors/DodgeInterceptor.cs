using System;

namespace GameLogic
{
    /// <summary>
    /// 闪避拦截器：当玩家请求闪避且可以移动时切换到闪避状态。
    /// </summary>
    public class DodgeInterceptor : PlayerStateInterceptor
    {
        public override int Priority => 100;

        public override bool TryIntercept(PlayerStateContext context, Type currentStateType, out StateTransitionRequest request)
        {
            request = null;

            if (!context.WantsToDodge) return false;
            if (!context.CanDodge) return false;
            if (currentStateType == typeof(PlayerDodgeState)) return false;

            request = new StateTransitionRequest(typeof(PlayerDodgeState), Priority);
            return true;
        }
    }
}
