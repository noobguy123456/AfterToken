using System;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 敌人死亡拦截器：HP 归零后强制进入死亡状态。
    /// </summary>
    public class EnemyDeathInterceptor : EnemyStateInterceptor
    {
        public override int Priority => int.MaxValue;

        public override bool TryIntercept(EnemyStateContext context, Type currentStateType, out StateTransitionRequest request)
        {
            request = null;
            if (currentStateType == typeof(EnemyDeadState)) return false;
            if (!context.IsDead) return false;
            request = new StateTransitionRequest(typeof(EnemyDeadState), Priority);
            return true;
        }
    }
}
