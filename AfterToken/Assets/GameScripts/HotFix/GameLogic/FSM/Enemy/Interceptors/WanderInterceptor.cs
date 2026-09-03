using System;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 敌人丢失目标拦截器：追击状态下玩家超出追踪距离则转散步。
    /// 仅对 Chase 生效（丢失判定）；散步/待机中重新发现玩家由 ChaseInterceptor 负责。
    /// </summary>
    public class EnemyWanderInterceptor : EnemyStateInterceptor
    {
        // 高于 Chase(100)、低于 Attack(200)：避免与追击请求同帧竞争导致抖动
        public override int Priority => 150;

        public override bool TryIntercept(EnemyStateContext context, Type currentStateType, out StateTransitionRequest request)
        {
            request = null;
            if (currentStateType != typeof(EnemyChaseState)) return false;
            if (!context.PlayerOutOfPursuit) return false;
            request = new StateTransitionRequest(typeof(EnemyWanderState), Priority);
            return true;
        }
    }
}
