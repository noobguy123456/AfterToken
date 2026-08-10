namespace GameLogic
{
    /// <summary>
    /// 道具堆叠。背包/仓库中的最小存储单元。
    /// [Serializable] 是 JsonUtility 存档序列化的硬性要求（缺少时列表字段会被静默跳过）。
    /// </summary>
    [System.Serializable]
    public struct ItemStack
    {
        /// <summary>
        /// 全局自增序号起点（跨背包/仓库共享，保证获取时间可比较）。
        /// </summary>
        private static long _nextSeq;

        /// <summary>
        /// 分配下一个获取序号。
        /// </summary>
        public static long NextSeq() => ++_nextSeq;

        /// <summary>
        /// 当前序号水位（存档用）。
        /// </summary>
        public static long CurrentSeq => _nextSeq;

        /// <summary>
        /// 从存档恢复序号水位。只升不降，避免与运行中已分配的序号冲突。
        /// </summary>
        public static void RestoreSeq(long value)
        {
            if (value > _nextSeq)
            {
                _nextSeq = value;
            }
        }

        /// <summary>
        /// 道具 ID（对应 cfg.Item.id）。
        /// </summary>
        public int ItemId;

        /// <summary>
        /// 堆叠数量。
        /// </summary>
        public int Count;

        /// <summary>
        /// 获取序号：进入当前容器（背包/仓库）的先后顺序，用于"按获取时间排序"。
        /// 堆叠合并时保留首个堆叠的序号（即首次获取时间）。
        /// </summary>
        public long AcquireSeq;

        public ItemStack(int itemId, int count)
        {
            ItemId = itemId;
            Count = count;
            AcquireSeq = NextSeq();
        }
    }
}
