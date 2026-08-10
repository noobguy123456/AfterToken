using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 狙击枪开镜模式设置（长按 / 切换）。
    /// 持久化由 SaveSystem 接管；旧 PlayerPrefs 值在首次读取时一次性导入。
    /// </summary>
    public static class SniperAimModeSetting
    {
        /// <summary>
        /// 旧 PlayerPrefs 键，仅用于迁移，不再写入。
        /// </summary>
        private const string LEGACY_KEY = "SniperAimMode";

        /// <summary>
        /// true = Toggle（点一下开镜、再点一下收镜）；false = Hold（按住开镜、松手收镜）。
        /// </summary>
        public static bool IsToggle
        {
            get
            {
                var d = SaveSystem.Data.settings;
                // 无存档时尝试导入旧 PlayerPrefs 值，否则默认 Hold
                return d.sniperAimModeInitialized
                    ? d.sniperAimModeToggle
                    : PlayerPrefs.GetInt(LEGACY_KEY, 0) == 1;
            }
            set
            {
                var d = SaveSystem.Data.settings;
                d.sniperAimModeInitialized = true;
                d.sniperAimModeToggle = value;
                SaveSystem.Flush();
            }
        }

        /// <summary>
        /// 保留给设置面板的"关闭时落盘"语义；变动即存模式下写入时已经落盘，这里无需再做什么。
        /// </summary>
        public static void Save()
        {
        }
    }
}
