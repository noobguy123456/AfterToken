using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家仓库（长期持有）。
    /// 持久化由 SaveSystem 接管（变动即存），首次访问时从存档懒加载。
    /// </summary>
    public static class Warehouse
    {
        private static readonly List<ItemStack> _items = new List<ItemStack>();
        private static bool _loaded;

        /// <summary>
        /// 最大槽位数（配置表）。
        /// </summary>
        public static int MaxSlots => InventoryConfigMgr.Instance.WarehouseCapacity;

        /// <summary>
        /// 已用槽位数。
        /// </summary>
        public static int UsedSlots { get { EnsureLoaded(); return _items.Count; } }

        /// <summary>
        /// 当前全部道具堆叠（只读）。
        /// </summary>
        public static IReadOnlyList<ItemStack> Items { get { EnsureLoaded(); return _items; } }

        /// <summary>
        /// 首次访问时从存档恢复（含获取序号水位）；无存档时为空仓库。
        /// </summary>
        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            var d = SaveSystem.Data.warehouse;
            if (!d.initialized) return;

            _items.Clear();
            if (d.items != null)
            {
                _items.AddRange(d.items);
            }
            ItemStack.RestoreSeq(d.nextSeq);
        }

        /// <summary>
        /// 变动即存：写回数据段并立即落盘。
        /// </summary>
        private static void Persist()
        {
            var d = SaveSystem.Data.warehouse;
            d.initialized = true;
            d.items.Clear();
            d.items.AddRange(_items);
            d.nextSeq = ItemStack.CurrentSeq;
            SaveSystem.Flush();
        }

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

            EnsureLoaded();

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

            if (_batchDepth == 0)
            {
                GameEvent.Get<IItemEvent>()?.OnWarehouseChanged();
                Persist();
            }
            return true;
        }

        /// <summary>
        /// 批量放入（胜利结算：临时背包整体转入仓库）。
        /// 批量期间抑制逐条事件与写盘，结束后统一触发一次。
        /// </summary>
        public static void AddAll(IReadOnlyList<ItemStack> stacks)
        {
            if (stacks == null)
            {
                return;
            }

            _batchDepth++;
            try
            {
                foreach (var stack in stacks)
                {
                    TryAdd(stack.ItemId, stack.Count);
                }
            }
            finally
            {
                _batchDepth--;
            }

            if (_batchDepth == 0)
            {
                GameEvent.Get<IItemEvent>()?.OnWarehouseChanged();
                Persist();
            }
        }

        // 批量操作深度：>0 时 TryAdd 不触发事件与写盘（见 AddAll）
        private static int _batchDepth;

        /// <summary>
        /// 清空仓库（调试用）。
        /// </summary>
        public static void Clear()
        {
            EnsureLoaded();
            if (_items.Count == 0)
            {
                return;
            }

            _items.Clear();
            GameEvent.Get<IItemEvent>()?.OnWarehouseChanged();
            Persist();
        }

        /// <summary>
        /// 整理仓库：排序规则与背包一致（稀有度 → 价值 → 获取时间）。
        /// </summary>
        public static void Organize()
        {
            EnsureLoaded();
            if (_items.Count <= 1)
            {
                return;
            }

            InventorySorter.Organize(_items);
            GameEvent.Get<IItemEvent>()?.OnWarehouseChanged();
            Persist();
        }

        /// <summary>
        /// 查询某物品当前总数量（跨所有堆叠）。
        /// </summary>
        public static int GetItemCount(int itemId)
        {
            EnsureLoaded();
            int total = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].ItemId == itemId)
                {
                    total += _items[i].Count;
                }
            }
            return total;
        }

        /// <summary>
        /// 是否拥有足够物品。
        /// </summary>
        public static bool HasItem(int itemId, int count)
        {
            if (count <= 0) return true;
            return GetItemCount(itemId) >= count;
        }

        /// <summary>
        /// 尝试消耗物品。不足时不产生部分扣除。
        /// </summary>
        public static bool TryConsume(int itemId, int count)
        {
            if (count <= 0) return true;
            // HasItem 内部已 EnsureLoaded
            if (!HasItem(itemId, count)) return false;

            int remaining = count;
            for (int i = _items.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var stack = _items[i];
                if (stack.ItemId != itemId) continue;

                int take = Mathf.Min(remaining, stack.Count);
                stack.Count -= take;
                remaining -= take;

                if (stack.Count <= 0)
                {
                    _items.RemoveAt(i);
                }
                else
                {
                    _items[i] = stack;
                }
            }

            GameEvent.Get<IItemEvent>()?.OnWarehouseChanged();
            Persist();
            return true;
        }
    }
}
