using System.Collections.Generic;
using System.Linq;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 传送门配置管理器（由 Luban 配置表驱动）。
    /// </summary>
    public class PortalConfigMgr
    {
        private static PortalConfigMgr _instance;
        public static PortalConfigMgr Instance => _instance ??= new PortalConfigMgr();

        private readonly Dictionary<int, PortalConfig> _configs = new Dictionary<int, PortalConfig>();
        private bool _loaded;
        // 表未就绪的告警只打一次，防止轮询调用刷屏；加载成功后重置以便重新导表后再报
        private bool _fallbackWarned;

        private PortalConfigMgr() { }

        private void EnsureLoaded()
        {
            if (_loaded) return;
            var table = ConfigSystem.Instance?.Tables?.TbPortal;
            if (table == null)
            {
                // 表未加载完成时不得闩锁 _loaded，否则首次抢跑后永久返回空表
                if (!_fallbackWarned)
                {
                    _fallbackWarned = true;
                    Log.Error("[PortalConfigMgr] TbPortal 未加载");
                }
                return;
            }

            foreach (var portal in table.DataList)
            {
                _configs[portal.Id] = new PortalConfig(portal);
            }
            _loaded = true;
            _fallbackWarned = false;
        }

        public PortalConfig Get(int id)
        {
            EnsureLoaded();
            _configs.TryGetValue(id, out var config);
            return config;
        }

        public IEnumerable<PortalConfig> GetAll()
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
