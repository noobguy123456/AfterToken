using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 道具相关事件接口。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IItemEvent
    {
        /// <summary>
        /// 拾取道具入临时背包时触发。
        /// </summary>
        void OnItemPickedUp(int itemId, int count);

        /// <summary>
        /// 临时背包内容或容量变化时触发（用于 UI 刷新）。
        /// </summary>
        void OnTempInventoryChanged(int usedSlots, int maxSlots);

        /// <summary>
        /// 仓库内容变化时触发。
        /// </summary>
        void OnWarehouseChanged();

        /// <summary>
        /// 临时背包已满、道具无法放入时触发。
        /// </summary>
        void OnInventoryFull();
    }
}
