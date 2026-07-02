using System;

namespace GameLogic
{
    /// <summary>
    /// 换弹拦截器：当玩家请求换弹且满足条件时切换到换弹状态。
    /// </summary>
    public class ReloadStartInterceptor : PlayerStateInterceptor
    {
        public override int Priority => 50;

        public override bool TryIntercept(PlayerStateContext context, Type currentStateType, out StateTransitionRequest request)
        {
            request = null;

            if (!context.WantsToReload) return false;
            if (currentStateType == typeof(PlayerReloadState)) return false;
            if (currentStateType == typeof(PlayerDeadState)) return false;
            if (currentStateType == typeof(PlayerDodgeState)) return false;

            var weapon = context.CurrentWeapon;
            if (weapon == null) return false;
            if (weapon.CurrentAmmo >= weapon.Config.clipSize) return false;
            if (weapon.IsReloading) return false;

            request = new StateTransitionRequest(typeof(PlayerReloadState), Priority);
            return true;
        }
    }
}
