using System.Collections.Generic;
using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 容器掉落配置管理器（TbLootContainer 包装）。
    /// </summary>
    public class LootContainerConfigMgr
    {
        private static LootContainerConfigMgr _instance;
        public static LootContainerConfigMgr Instance => _instance ??= new LootContainerConfigMgr();

        private readonly List<LootContainer> _resultCache = new List<LootContainer>();

        /// <summary>
        /// 获取指定容器类型的全部掉落记录（无记录时返回空列表）。
        /// 返回的内部列表请勿缓存或修改。
        /// </summary>
        public List<LootContainer> GetRowsForContainer(int containerId)
        {
            _resultCache.Clear();
            var table = ConfigSystem.Instance.Tables.TbLootContainer;
            foreach (var row in table.DataList)
            {
                if (row.ContainerId == containerId)
                {
                    _resultCache.Add(row);
                }
            }
            return _resultCache;
        }
    }
}
