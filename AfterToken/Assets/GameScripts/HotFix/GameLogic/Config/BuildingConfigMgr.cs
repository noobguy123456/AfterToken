using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 建筑配置管理器。
    /// </summary>
    public class BuildingConfigMgr
    {
        private static BuildingConfigMgr _instance;
        public static BuildingConfigMgr Instance => _instance ??= new BuildingConfigMgr();

        public Building Get(int buildingId)
        {
            return ConfigSystem.Instance.Tables.TbBuilding.GetOrDefault(buildingId);
        }

        public System.Collections.Generic.IReadOnlyList<Building> GetAll()
        {
            return ConfigSystem.Instance.Tables.TbBuilding.DataList;
        }
    }
}
