using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 经营时间配置管理器。
    /// </summary>
    public class SimTimeConfigMgr
    {
        private const int CONFIG_ID = 1;

        private static SimTimeConfigMgr _instance;
        public static SimTimeConfigMgr Instance => _instance ??= new SimTimeConfigMgr();

        private SimTimeConfig _cachedConfig;

        private SimTimeConfig Get()
        {
            if (_cachedConfig == null)
            {
                _cachedConfig = ConfigSystem.Instance.Tables.TbSimTimeConfig.GetOrDefault(CONFIG_ID);
            }
            return _cachedConfig;
        }

        public float BaseSpeed => Get()?.BaseSpeed ?? 1f;
        public float FastSpeed => Get()?.FastSpeed ?? 2f;
        public float MaxSpeed => Get()?.MaxSpeed ?? 4f;
        public float OrderRefreshInterval => Get()?.OrderRefreshInterval ?? 30f;
        public int MaxOrderCount => Get()?.MaxOrderCount ?? 5;
    }
}
