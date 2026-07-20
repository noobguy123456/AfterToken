using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 关卡内临时背包（一局战斗的生命周期）。
    /// 槽位制：一个道具堆叠占 1 槽，堆叠上限来自 cfg.Item.stackLimit。
    /// 生命周期规则：传送门跨战斗场景保留；死亡清空；经 RETURN_TO_LOBBY 传送门离开时转入仓库。
    /// </summary>
    public static class RunInventory
    {
        private static readonly List<ItemStack> _items = new List<ItemStack>();

        /// <summary>
        /// 最大槽位数（配置表）。
        /// </summary>
        public static int MaxSlots => InventoryConfigMgr.Instance.TempBagCapacity;

        /// <summary>
        /// 已用槽位数。
        /// </summary>
        public static int UsedSlots => _items.Count;

        /// <summary>
        /// 当前全部道具堆叠（只读）。
        /// </summary>
        public static IReadOnlyList<ItemStack> Items => _items;

        /// <summary>
        /// 尝试放入一批道具。优先填充已有堆叠，不足时占用新槽位。
        /// 槽位不足以放下整批时整批失败（返回 false），掉落物可留在地上稍后拾取。
        /// </summary>
        public static bool TryAdd(int itemId, int count)
        {
            if (count <= 0 || ItemConfigMgr.Instance.Get(itemId) == null)
            {
                return false;
            }

            int stackLimit = ItemConfigMgr.Instance.GetStackLimit(itemId);
            int remaining = count;

            // 先填充已有堆叠
            for (int i = 0; i < _items.Count && remaining > 0; i++)
            {
                var stack = _items[i];
                if (stack.ItemId != itemId || stack.Count >= stackLimit)
                {
                    continue;
                }

                int add = Mathf.Min(remaining, stackLimit - stack.Count);
                stack.Count += add;
                remaining -= add;
                _items[i] = stack;
            }

            // 再占用新槽位
            if (remaining > 0)
            {
                int needSlots = (remaining + stackLimit - 1) / stackLimit;
                if (UsedSlots + needSlots > MaxSlots)
                {
                    GameEvent.Get<IItemEvent>().OnInventoryFull();
                    return false;
                }

                while (remaining > 0)
                {
                    int add = Mathf.Min(remaining, stackLimit);
                    _items.Add(new ItemStack(itemId, add));
                    remaining -= add;
                }
            }

            NotifyChanged();
            return true;
        }

        /// <summary>
        /// 清空背包（死亡 / 新一局开始 / 转入仓库后调用）。
        /// </summary>
        public static void Clear()
        {
            if (_items.Count == 0)
            {
                return;
            }

            _items.Clear();
            NotifyChanged();
        }

        private static void NotifyChanged()
        {
            GameEvent.Get<IItemEvent>().OnTempInventoryChanged(UsedSlots, MaxSlots);
        }
    }
}
