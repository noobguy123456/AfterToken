namespace GameLogic
{
    /// <summary>
    /// 背包容量配置管理器。
    /// </summary>
    public class InventoryConfigMgr
    {
        private const int CONFIG_ID = 1;

        private static InventoryConfigMgr _instance;
        public static InventoryConfigMgr Instance => _instance ??= new InventoryConfigMgr();

        /// <summary>
        /// 临时背包最大槽位数（配置缺失时默认 12）。
        /// </summary>
        public int TempBagCapacity
        {
            get
            {
                var cfg = ConfigSystem.Instance.Tables.TbInventoryConfig.GetOrDefault(CONFIG_ID);
                return cfg != null && cfg.TempBagCapacity > 0 ? cfg.TempBagCapacity : 12;
            }
        }

        /// <summary>
        /// 仓库最大槽位数（配置缺失时默认 200）。
        /// </summary>
        public int WarehouseCapacity
        {
            get
            {
                var cfg = ConfigSystem.Instance.Tables.TbInventoryConfig.GetOrDefault(CONFIG_ID);
                return cfg != null && cfg.WarehouseCapacity > 0 ? cfg.WarehouseCapacity : 200;
            }
        }
    }
}
