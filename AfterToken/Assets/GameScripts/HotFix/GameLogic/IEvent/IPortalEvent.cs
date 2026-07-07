using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 传送门事件接口。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IPortalEvent
    {
        /// <summary>
        /// 传送门被触发，即将切换场景。
        /// </summary>
        /// <param name="configId">传送门配置 ID。</param>
        /// <param name="portalType">传送门类型。</param>
        /// <param name="targetScene">目标场景名。</param>
        void OnPortalTriggered(int configId, string portalType, string targetScene);

        /// <summary>
        /// 传送门从非激活变为激活（条件达成）。
        /// </summary>
        /// <param name="configId">传送门配置 ID。</param>
        void OnPortalActivated(int configId);

        /// <summary>
        /// 玩家进入传送门触发区域。
        /// </summary>
        /// <param name="configId">传送门配置 ID。</param>
        void OnPortalEntered(int configId);

        /// <summary>
        /// 玩家离开传送门触发区域。
        /// </summary>
        /// <param name="configId">传送门配置 ID。</param>
        void OnPortalExited(int configId);
    }
}
