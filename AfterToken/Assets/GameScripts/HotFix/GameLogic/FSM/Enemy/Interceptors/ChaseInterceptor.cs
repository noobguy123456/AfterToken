using System;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 敌人追击拦截器：玩家进入侦测范围且未在攻击范围则追击。
    /// </summary>
    public class EnemyChaseInterceptor : EnemyStateInterceptor
    {
        public override int Priority => 100;

        public override bool TryIntercept(EnemyStateContext context, Type currentStateType, out StateTransitionRequest request)
        {
            request = null;
            if (currentStateType == typeof(EnemyDeadState)) return false;
            if (context.WantsToAttack) return false;
            if (!context.WantsToChase) return false;
            if (currentStateType == typeof(EnemyChaseState)) return false;
            request = new StateTransitionRequest(typeof(EnemyChaseState), Priority);
            return true;
        }
    }
}
