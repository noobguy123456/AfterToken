using System;

namespace GameLogic
{
    /// <summary>
    /// 死亡拦截器：当玩家死亡时强制切换到死亡状态。
    /// </summary>
    public class DeathInterceptor : PlayerStateInterceptor
    {
        public override int Priority => 1000;

        public override bool TryIntercept(PlayerStateContext context, Type currentStateType, out StateTransitionRequest request)
        {
            request = null;

            if (!context.IsDead) return false;
            if (currentStateType == typeof(PlayerDeadState)) return false;

            request = new StateTransitionRequest(typeof(PlayerDeadState), Priority);
            return true;
        }
    }
}
