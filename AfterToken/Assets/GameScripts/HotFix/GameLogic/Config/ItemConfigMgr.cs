using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 道具配置管理器。
    /// 业务代码不直接访问 Tables.TbItem，统一从这里取，隔离配表结构变更。
    /// </summary>
    public class ItemConfigMgr
    {
        private static ItemConfigMgr _instance;
        public static ItemConfigMgr Instance => _instance ??= new ItemConfigMgr();

        /// <summary>
        /// 按 ID 获取道具配置，不存在返回 null。
        /// </summary>
        public Item Get(int itemId)
        {
            return ConfigSystem.Instance.Tables.TbItem.GetOrDefault(itemId);
        }

        /// <summary>
        /// 获取道具名称（找不到配置时返回占位文本）。
        /// </summary>
        public string GetName(int itemId)
        {
            return Get(itemId)?.Name ?? $"Item#{itemId}";
        }

        /// <summary>
        /// 获取道具稀有度（找不到配置时按最低档 Blue 处理）。
        /// </summary>
        public EQuality GetQuality(int itemId)
        {
            return Get(itemId)?.Quality ?? EQuality.Blue;
        }

        /// <summary>
        /// 获取道具堆叠上限（未配置或配置异常时按 1 处理）。
        /// </summary>
        public int GetStackLimit(int itemId)
        {
            var item = Get(itemId);
            return item != null && item.StackLimit > 0 ? item.StackLimit : 1;
        }
    }
}
