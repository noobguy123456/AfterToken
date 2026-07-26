using System.Collections.Generic;
using GameConfig.cfg;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 背包/仓库通用查询与消耗接口，包装 <see cref="Warehouse"/>。
    /// 经营系统通过本类与库存交互，避免直接操作 <see cref="Warehouse"/>。
    /// </summary>
    public static class InventorySystem
    {
        /// <summary>
        /// 查询某物品当前总数量（跨所有堆叠）。
        /// </summary>
        public static int GetItemCount(int itemId)
        {
            return Warehouse.GetItemCount(itemId);
        }

        /// <summary>
        /// 是否拥有足够物品。
        /// </summary>
        public static bool HasItem(int itemId, int count)
        {
            return Warehouse.HasItem(itemId, count);
        }

        /// <summary>
        /// 是否拥有足够的一批物品。
        /// </summary>
        public static bool HasItems(IReadOnlyList<ItemExchange> items)
        {
            if (items == null) return true;
            foreach (var item in items)
            {
                if (!HasItem(item.Id, item.Num))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 尝试消耗一批物品。任一物品不足时整体失败，不产生部分扣除。
        /// </summary>
        public static bool TryConsumeItems(IReadOnlyList<ItemExchange> items)
        {
            if (items == null || items.Count == 0) return true;
            if (!HasItems(items)) return false;

            foreach (var item in items)
            {
                Warehouse.TryConsume(item.Id, item.Num);
            }
            GameEvent.Get<IInventoryEvent>().OnItemChanged(0, 0);
            return true;
        }

        /// <summary>
        /// 尝试消耗单个物品。
        /// </summary>
        public static bool TryConsumeItem(int itemId, int count)
        {
            if (count <= 0) return true;
            if (!HasItem(itemId, count)) return false;
            Warehouse.TryConsume(itemId, count);
            GameEvent.Get<IInventoryEvent>().OnItemChanged(itemId, GetItemCount(itemId));
            return true;
        }

        /// <summary>
        /// 添加物品到仓库。
        /// </summary>
        public static bool AddItem(int itemId, int count)
        {
            return Warehouse.TryAdd(itemId, count);
        }

        /// <summary>
        /// 批量添加物品到仓库。
        /// </summary>
        public static void AddItems(IReadOnlyList<ItemExchange> items)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                Warehouse.TryAdd(item.Id, item.Num);
            }
            GameEvent.Get<IInventoryEvent>().OnItemChanged(0, 0);
        }
    }
}
