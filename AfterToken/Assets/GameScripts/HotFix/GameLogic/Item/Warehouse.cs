using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家仓库（长期持有）。
    /// 本期为内存态：重启游戏即清空，持久化由 save-system 模块统一实现。
    /// </summary>
    public static class Warehouse
    {
        private static readonly List<ItemStack> _items = new List<ItemStack>();

        /// <summary>
        /// 最大槽位数（配置表）。
        /// </summary>
        public static int MaxSlots => InventoryConfigMgr.Instance.WarehouseCapacity;

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
        /// 仓库满时返回 false 并丢弃放不下的部分（记录日志）。
        /// </summary>
        public static bool TryAdd(int itemId, int count)
        {
            if (count <= 0 || ItemConfigMgr.Instance.Get(itemId) == null)
            {
                return false;
            }

            int stackLimit = ItemConfigMgr.Instance.GetStackLimit(itemId);
            int remaining = count;

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

            if (remaining > 0)
            {
                int needSlots = (remaining + stackLimit - 1) / stackLimit;
                if (UsedSlots + needSlots > MaxSlots)
                {
                    Log.Warning($"[Warehouse] 仓库已满，{ItemConfigMgr.Instance.GetName(itemId)} x{remaining} 未能入库");
                    return false;
                }

                while (remaining > 0)
                {
                    int add = Mathf.Min(remaining, stackLimit);
                    _items.Add(new ItemStack(itemId, add));
                    remaining -= add;
                }
            }

            GameEvent.Get<IItemEvent>().OnWarehouseChanged();
            return true;
        }

        /// <summary>
        /// 批量放入（胜利结算：临时背包整体转入仓库）。
        /// </summary>
        public static void AddAll(IReadOnlyList<ItemStack> stacks)
        {
            if (stacks == null)
            {
                return;
            }

            foreach (var stack in stacks)
            {
                TryAdd(stack.ItemId, stack.Count);
            }
        }

        /// <summary>
        /// 清空仓库（调试用）。
        /// </summary>
        public static void Clear()
        {
            if (_items.Count == 0)
            {
                return;
            }

            _items.Clear();
            GameEvent.Get<IItemEvent>().OnWarehouseChanged();
        }
    }
}
