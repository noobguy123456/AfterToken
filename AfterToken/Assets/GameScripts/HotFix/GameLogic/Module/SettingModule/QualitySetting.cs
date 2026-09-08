using System;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 画质档位设置（QualitySettings 档位索引）。
    /// 持久化由 SaveSystem 接管；写入时立即应用并落盘。
    /// </summary>
    public static class QualitySetting
    {
        private static int? _cachedLevel;

        /// <summary>
        /// 当前画质档位索引（0..QualitySettings.names.Length-1）。
        /// 无存档时以引擎当前档位为默认（编辑器/平台 Graphics 设置决定）。
        /// </summary>
        public static int Level
        {
            get
            {
                if (!_cachedLevel.HasValue)
                {
                    var d = SaveSystem.Data.settings;
                    _cachedLevel = d.qualityLevelInitialized ? d.qualityLevel : QualitySettings.GetQualityLevel();
                }
                return ClampLevel(_cachedLevel.Value);
            }
            set
            {
                _cachedLevel = ClampLevel(value);

                var d = SaveSystem.Data.settings;
                d.qualityLevelInitialized = true;
                d.qualityLevel = _cachedLevel.Value;
                SaveSystem.Flush();

                QualitySettings.SetQualityLevel(_cachedLevel.Value, true);
                OnChanged?.Invoke();
            }
        }

        /// <summary>
        /// 档位总数（= QualitySettings.names.Length）。
        /// </summary>
        public static int LevelCount => QualitySettings.names.Length;

        /// <summary>
        /// 画质档位变动事件（设置面板等 UI 刷新用）。
        /// </summary>
        public static event Action OnChanged;

        /// <summary>
        /// 取档位显示名（引擎侧档位名，如 "Medium"）。
        /// </summary>
        public static string GetLevelName(int level)
        {
            var names = QualitySettings.names;
            if (names.Length == 0)
            {
                return string.Empty;
            }
            return names[ClampLevel(level)];
        }

        /// <summary>
        /// 启动/槽位切换时把存档中的画质档位读回应用到 QualitySettings。
        /// </summary>
        public static void Apply()
        {
            QualitySettings.SetQualityLevel(Level, true);
        }

        private static int ClampLevel(int level)
        {
            int count = QualitySettings.names.Length;
            return count > 0 ? Mathf.Clamp(level, 0, count - 1) : 0;
        }

        /// <summary>
        /// 失效缓存（存档槽位切换时由 SaveSystem 调用），下次访问从新槽位重读。
        /// </summary>
        public static void InvalidateCache()
        {
            _cachedLevel = null;
        }
    }
}
