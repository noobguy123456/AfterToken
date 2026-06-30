using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 准星灵敏度设置。
    /// 使用 PlayerPrefs 持久化，供游戏内设置面板读写。
    /// </summary>
    public static class SensitivitySetting
    {
        private const string Key = "CrosshairSensitivity";
        private const float DefaultValue = 1f;
        private const float MinValue = 0.1f;
        private const float MaxValue = 15f;

        private static float? _cachedValue;

        public static float Value
        {
            get
            {
                if (!_cachedValue.HasValue)
                {
                    _cachedValue = PlayerPrefs.GetFloat(Key, DefaultValue);
                }
                return Mathf.Clamp(_cachedValue.Value, MinValue, MaxValue);
            }
            set
            {
                _cachedValue = Mathf.Clamp(value, MinValue, MaxValue);
                PlayerPrefs.SetFloat(Key, _cachedValue.Value);
                PlayerPrefs.Save();
            }
        }

        public static float Min => MinValue;
        public static float Max => MaxValue;
        public static float Default => DefaultValue;
    }
}
