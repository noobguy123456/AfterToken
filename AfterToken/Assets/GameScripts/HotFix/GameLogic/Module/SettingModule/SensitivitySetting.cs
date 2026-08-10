using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 准星灵敏度设置。
    /// 持久化由 SaveSystem 接管；旧 PlayerPrefs 值在首次读取时一次性导入。
    /// </summary>
    public static class SensitivitySetting
    {
        /// <summary>
        /// 旧 PlayerPrefs 键，仅用于迁移，不再写入。
        /// </summary>
        private const string LEGACY_KEY = "CrosshairSensitivity";
        private const float DEFAULT_VALUE = 1f;
        private const float MIN_VALUE = 0.01f;
        private const float MAX_VALUE = 100f;

        private static float? _cachedValue;
        private static float? _cachedScopeValue;

        public static float Value
        {
            get
            {
                if (!_cachedValue.HasValue)
                {
                    var d = SaveSystem.Data.settings;
                    // 无存档时尝试导入旧 PlayerPrefs 值，否则用默认值
                    _cachedValue = d.sensitivityInitialized
                        ? d.sensitivity
                        : PlayerPrefs.GetFloat(LEGACY_KEY, DEFAULT_VALUE);
                }
                return Mathf.Clamp(_cachedValue.Value, MIN_VALUE, MAX_VALUE);
            }
            set
            {
                _cachedValue = Mathf.Clamp(value, MIN_VALUE, MAX_VALUE);

                var d = SaveSystem.Data.settings;
                d.sensitivityInitialized = true;
                d.sensitivity = _cachedValue.Value;
                SaveSystem.Flush();
            }
        }

        /// <summary>
        /// 开镜（狙击镜）灵敏度，独立于普通灵敏度，互不影响。
        /// </summary>
        public static float ScopedValue
        {
            get
            {
                if (!_cachedScopeValue.HasValue)
                {
                    var d = SaveSystem.Data.settings;
                    // 无存档时跟随普通灵敏度（手感一致，玩家之后再按需调低）；
                    // 不用固定默认值——普通灵敏度本身可调，固定值会和它严重脱节
                    _cachedScopeValue = d.scopeSensitivityInitialized
                        ? d.scopeSensitivity
                        : Value;
                }
                return Mathf.Clamp(_cachedScopeValue.Value, MIN_VALUE, MAX_VALUE);
            }
            set
            {
                _cachedScopeValue = Mathf.Clamp(value, MIN_VALUE, MAX_VALUE);

                var d = SaveSystem.Data.settings;
                d.scopeSensitivityInitialized = true;
                d.scopeSensitivity = _cachedScopeValue.Value;
                SaveSystem.Flush();
            }
        }

        public static float Min => MIN_VALUE;
        public static float Max => MAX_VALUE;
        public static float Default => DEFAULT_VALUE;

        /// <summary>
        /// 保留给设置面板的"关闭时落盘"语义；变动即存模式下写入时已经落盘，这里无需再做什么。
        /// </summary>
        public static void Save()
        {
        }
    }
}
