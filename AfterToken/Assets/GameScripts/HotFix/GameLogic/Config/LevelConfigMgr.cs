using System.Collections.Generic;
using System.Linq;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 关卡配置管理器（由 Luban 配置表驱动）。
    /// </summary>
    public class LevelConfigMgr
    {
        private static LevelConfigMgr _instance;
        public static LevelConfigMgr Instance => _instance ??= new LevelConfigMgr();

        private readonly Dictionary<int, LevelConfig> _configs = new Dictionary<int, LevelConfig>();
        private bool _loaded;

        private LevelConfigMgr() { }

        private void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            var table = ConfigSystem.Instance.Tables.TbLevel;
            if (table == null)
            {
                Log.Error("[LevelConfigMgr] TbLevel 未加载");
                return;
            }
            foreach (var level in table.DataList)
            {
                _configs[level.Id] = new LevelConfig(level);
            }
        }

        public LevelConfig Get(int id)
        {
            EnsureLoaded();
            _configs.TryGetValue(id, out var config);
            return config;
        }

        public IEnumerable<LevelConfig> GetAll()
        {
            EnsureLoaded();
            return _configs.Values.OrderBy(c => c.id);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// GM：重新加载。
        /// </summary>
        public void Reload()
        {
            _configs.Clear();
            _loaded = false;
        }
#endif
    }
}
