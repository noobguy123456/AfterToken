using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 通用背包/仓库物品变化事件接口。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IInventoryEvent
    {
        void OnItemChanged(int itemId, int count);
    }
}
