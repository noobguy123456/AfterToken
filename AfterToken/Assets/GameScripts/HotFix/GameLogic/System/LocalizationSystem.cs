using System;
using System.Collections.Generic;
using GameConfig.cfg;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 多语言系统（Luban 词条表驱动）。
    /// 设计文档：docs/Proposal/infra/localization.md。
    /// 数据源：TbLocalization（Configs/GameConfig/Datas/localization.csv）。
    /// 取词兜底链：当前语言列 → en → key 原文 + Warning。
    /// </summary>
    public class LocalizationSystem
    {
        private static LocalizationSystem _instance;
        public static LocalizationSystem Instance => _instance ??= new LocalizationSystem();

        /// <summary>
        /// 语言切换事件（UI 绑定刷新钩子）。
        /// </summary>
        public event Action OnLanguageChanged;

        /// <summary>
        /// 当前语言（TEngine.Language 枚举，与存档/ProcedureLaunch 保持一致）。
        /// </summary>
        public Language Current { get; private set; } = Language.English;

        private readonly Dictionary<string, Localization> _entries = new Dictionary<string, Localization>();
        private bool _initialized;

        /// <summary>
        /// 当前支持在设置界面展示的语言。只列出词条表已有内容的列，
        /// 其余语言列（zh_tw/ja/ko）填了内容后再加进来。
        /// </summary>
        public static readonly Language[] SupportedLanguages =
        {
            Language.English,
            Language.ChineseSimplified,
        };

        /// <summary>
        /// 初始化：建词条索引并读取存档语言。须在 ConfigSystem.LoadAsync 完成后调用。
        /// </summary>
        public void Initialize()
        {
            _entries.Clear();
            foreach (var row in ConfigSystem.Instance.Tables.TbLocalization.DataList)
            {
                if (string.IsNullOrEmpty(row.Key))
                {
                    continue;
                }
                _entries[row.Key] = row;
            }

            Current = LoadSavedLanguage();
            _initialized = true;
            Log.Info($"[LocalizationSystem] 初始化完成，词条数={_entries.Count}，当前语言={Current}");
        }

        /// <summary>
        /// 切换语言：写存档并触发刷新事件。
        /// </summary>
        public void SetLanguage(Language language)
        {
            if (Current == language && _initialized)
            {
                return;
            }

            Current = language;
            Utility.PlayerPrefs.SetString(Constant.Setting.Language, language.ToString());
            Utility.PlayerPrefs.Save();
            Log.Info($"[LocalizationSystem] 切换语言 = {language}");
            OnLanguageChanged?.Invoke();
        }

        /// <summary>
        /// 取词。缺失时返回 key 原文并告警（开发期可辨）。
        /// </summary>
        public string GetText(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            if (!_entries.TryGetValue(key, out var row))
            {
                Log.Warning($"[LocalizationSystem] 缺少词条: {key}");
                return key;
            }

            string text = GetColumn(row, Current);
            if (string.IsNullOrEmpty(text))
            {
                text = row.En;
            }
            if (string.IsNullOrEmpty(text))
            {
                Log.Warning($"[LocalizationSystem] 词条无英文兜底: {key}");
                return key;
            }
            return text;
        }

        /// <summary>
        /// 取词并参数化（词条内用 {0} {1} 占位）。
        /// </summary>
        public string GetText(string key, params object[] args)
        {
            string text = GetText(key);
            if (args == null || args.Length == 0)
            {
                return text;
            }
            try
            {
                return string.Format(text, args);
            }
            catch (FormatException e)
            {
                Log.Warning($"[LocalizationSystem] 词条格式化失败: {key}, {e.Message}");
                return text;
            }
        }

        /// <summary>
        /// 语言的显示名（用于设置界面下拉，显示该语言自己的叫法）。
        /// </summary>
        public static string GetLanguageDisplayName(Language language)
        {
            switch (language)
            {
                case Language.English: return "English";
                case Language.ChineseSimplified: return "简体中文";
                case Language.ChineseTraditional: return "繁體中文";
                case Language.Japanese: return "日本語";
                case Language.Korean: return "한국어";
                default: return language.ToString();
            }
        }

        private static string GetColumn(Localization row, Language language)
        {
            switch (language)
            {
                case Language.ChineseSimplified: return row.ZhCn;
                case Language.ChineseTraditional: return row.ZhTw;
                case Language.Japanese: return row.Ja;
                case Language.Korean: return row.Ko;
                default: return row.En;
            }
        }

        private static Language LoadSavedLanguage()
        {
            if (Utility.PlayerPrefs.HasSetting(Constant.Setting.Language))
            {
                try
                {
                    string saved = Utility.PlayerPrefs.GetString(Constant.Setting.Language);
                    return (Language)Enum.Parse(typeof(Language), saved);
                }
                catch (Exception e)
                {
                    Log.Warning($"[LocalizationSystem] 存档语言解析失败: {e.Message}");
                }
            }
            return Language.English;
        }
    }
}
