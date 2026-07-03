using System;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 敌人攻击拦截器：玩家进入攻击范围则尝试攻击。
    /// </summary>
    public class EnemyAttackInterceptor : EnemyStateInterceptor
    {
        public override int Priority => 200;

        public override bool TryIntercept(EnemyStateContext context, Type currentStateType, out StateTransitionRequest request)
        {
            request = null;
            if (currentStateType == typeof(EnemyDeadState)) return false;
            if (!context.WantsToAttack) return false;
            if (currentStateType == typeof(EnemyAttackState)) return false;
            request = new StateTransitionRequest(typeof(EnemyAttackState), Priority);
            return true;
        }
    }
}
