using System;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 敌人状态拦截器基类。
    /// </summary>
    public abstract class EnemyStateInterceptor
    {
        public abstract int Priority { get; }

        public abstract bool TryIntercept(EnemyStateContext context, Type currentStateType, out StateTransitionRequest request);
    }
}
