using System;

namespace GameLogic
{
    /// <summary>
    /// 状态切换请求。
    /// </summary>
    public class StateTransitionRequest
    {
        /// <summary>
        /// 目标状态类型。
        /// </summary>
        public Type TargetStateType { get; }

        /// <summary>
        /// 优先级。
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// 用户数据。
        /// </summary>
        public object UserData { get; }

        public StateTransitionRequest(Type targetStateType, int priority = 0, object userData = null)
        {
            TargetStateType = targetStateType;
            Priority = priority;
            UserData = userData;
        }
    }
}
