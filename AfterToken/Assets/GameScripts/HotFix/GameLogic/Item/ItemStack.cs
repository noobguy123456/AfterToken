namespace GameLogic
{
    /// <summary>
    /// 道具堆叠。背包/仓库中的最小存储单元。
    /// </summary>
    public struct ItemStack
    {
        /// <summary>
        /// 道具 ID（对应 cfg.Item.id）。
        /// </summary>
        public int ItemId;

        /// <summary>
        /// 堆叠数量。
        /// </summary>
        public int Count;

        public ItemStack(int itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
    }
}
