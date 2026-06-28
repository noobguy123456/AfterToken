using System.Collections.Generic;
using System.Linq;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 武器配置管理器（由 Luban 配置表驱动）。
    /// </summary>
    public class WeaponConfigMgr
    {
        private static WeaponConfigMgr _instance;
        public static WeaponConfigMgr Instance => _instance ??= new WeaponConfigMgr();

        private readonly Dictionary<int, WeaponConfig> _configs = new Dictionary<int, WeaponConfig>();
        private bool _loaded;

        private WeaponConfigMgr() { }

        private void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            var table = ConfigSystem.Instance.Tables.TbWeapon;
            if (table == null)
            {
                Log.Error("[WeaponConfigMgr] TbWeapon 未加载");
                return;
            }
            foreach (var pair in table.DataMap)
            {
                _configs[pair.Key] = new WeaponConfig(pair.Value);
            }
        }

        public WeaponConfig Get(int id)
        {
            EnsureLoaded();
            _configs.TryGetValue(id, out var config);
            return config;
        }

        public IEnumerable<WeaponConfig> GetAll()
        {
            EnsureLoaded();
            return _configs.Values;
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
