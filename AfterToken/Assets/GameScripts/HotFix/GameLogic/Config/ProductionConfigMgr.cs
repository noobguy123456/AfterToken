using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 生产配方配置管理器。
    /// </summary>
    public class ProductionConfigMgr
    {
        private static ProductionConfigMgr _instance;
        public static ProductionConfigMgr Instance => _instance ??= new ProductionConfigMgr();

        public Production Get(int productionId)
        {
            return ConfigSystem.Instance.Tables.TbProduction.GetOrDefault(productionId);
        }

        public System.Collections.Generic.IReadOnlyList<Production> GetAll()
        {
            return ConfigSystem.Instance.Tables.TbProduction.DataList;
        }

        public System.Collections.Generic.List<Production> GetByBuildingId(int buildingId)
        {
            var result = new System.Collections.Generic.List<Production>();
            var all = GetAll();
            foreach (var p in all)
            {
                if (p.BuildingId == buildingId)
                {
                    result.Add(p);
                }
            }
            return result;
        }
    }
}
