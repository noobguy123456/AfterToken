using System;

namespace GameLogic
{
    /// <summary>
    /// 玩家状态拦截器基类。
    /// 用于统一处理全局状态切换条件（如死亡、闪避、换弹等）。
    /// </summary>
    public abstract class PlayerStateInterceptor
    {
        /// <summary>
        /// 拦截器优先级，数值越大越先执行。
        /// </summary>
        public virtual int Priority => 0;

        /// <summary>
        /// 尝试拦截当前状态切换。
        /// </summary>
        /// <param name="context">玩家状态黑板</param>
        /// <param name="currentStateType">当前状态类型</param>
        /// <param name="request">输出：希望切换到的目标状态请求</param>
        /// <returns>是否拦截成功</returns>
        public abstract bool TryIntercept(
            PlayerStateContext context,
            Type currentStateType,
            out StateTransitionRequest request);
    }
}
