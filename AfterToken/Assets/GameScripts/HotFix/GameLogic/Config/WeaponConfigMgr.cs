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
        // 表未就绪的告警只打一次，防止轮询调用刷屏；加载成功后重置以便重新导表后再报
        private bool _fallbackWarned;

        private WeaponConfigMgr() { }

        private void EnsureLoaded()
        {
            if (_loaded) return;
            var table = ConfigSystem.Instance?.Tables?.TbWeapon;
            if (table == null)
            {
                // 表未加载完成时不得闩锁 _loaded，否则首次抢跑后永久返回空表
                if (!_fallbackWarned)
                {
                    _fallbackWarned = true;
                    Log.Error("[WeaponConfigMgr] TbWeapon 未加载");
                }
                return;
            }
            foreach (var pair in table.DataMap)
            {
                _configs[pair.Key] = new WeaponConfig(pair.Value);
            }
            _loaded = true;
            _fallbackWarned = false;
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
