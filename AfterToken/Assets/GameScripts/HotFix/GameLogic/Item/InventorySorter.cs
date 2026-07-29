using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 容器整理排序工具（搜打撤规则）。
    /// 排序优先级：稀有度从高到低 → 道具价值从大到小 → 获取时间从早到晚。
    /// 背包（RunInventory）与仓库（Warehouse）共用同一套规则。
    /// </summary>
    public static class InventorySorter
    {
        /// <summary>
        /// 对堆叠列表原地执行整理排序。
        /// </summary>
        public static void Organize(List<ItemStack> items)
        {
            if (items == null || items.Count <= 1)
            {
                return;
            }

            items.Sort(Compare);
        }

        private static int Compare(ItemStack a, ItemStack b)
        {
            // 稀有度：枚举值越大越稀有，从高到低
            int qualityA = (int)ItemConfigMgr.Instance.GetQuality(a.ItemId);
            int qualityB = (int)ItemConfigMgr.Instance.GetQuality(b.ItemId);
            if (qualityA != qualityB)
            {
                return qualityB.CompareTo(qualityA);
            }

            // 价值：从大到小
            int priceA = ItemConfigMgr.Instance.GetPrice(a.ItemId);
            int priceB = ItemConfigMgr.Instance.GetPrice(b.ItemId);
            if (priceA != priceB)
            {
                return priceB.CompareTo(priceA);
            }

            // 获取时间：从早到晚（序号小的在前）
            return a.AcquireSeq.CompareTo(b.AcquireSeq);
        }
    }
}
