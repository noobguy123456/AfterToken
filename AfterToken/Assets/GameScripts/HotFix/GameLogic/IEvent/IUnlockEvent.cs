using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 内容解锁事件接口。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IUnlockEvent
    {
        /// <summary>
        /// 内容解锁时触发（TbUnlock 记录 ID）。
        /// </summary>
        void OnContentUnlocked(int unlockId);
    }
}
