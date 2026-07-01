using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 准星灵敏度设置。
    /// 使用 PlayerPrefs 持久化，供游戏内设置面板读写。
    /// </summary>
    public static class SensitivitySetting
    {
        private const string KEY = "CrosshairSensitivity";
        private const float DEFAULT_VALUE = 1f;
        private const float MIN_VALUE = 0.01f;
        private const float MAX_VALUE = 100f;

        private static float? _cachedValue;

        public static float Value
        {
            get
            {
                if (!_cachedValue.HasValue)
                {
                    _cachedValue = PlayerPrefs.GetFloat(KEY, DEFAULT_VALUE);
                }
                return Mathf.Clamp(_cachedValue.Value, MIN_VALUE, MAX_VALUE);
            }
            set
            {
                _cachedValue = Mathf.Clamp(value, MIN_VALUE, MAX_VALUE);
                PlayerPrefs.SetFloat(KEY, _cachedValue.Value);
            }
        }

        public static float Min => MIN_VALUE;
        public static float Max => MAX_VALUE;
        public static float Default => DEFAULT_VALUE;

        /// <summary>
        /// 将当前设置写入磁盘。建议在设置面板关闭时调用一次，避免频繁写盘。
        /// </summary>
        public static void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
