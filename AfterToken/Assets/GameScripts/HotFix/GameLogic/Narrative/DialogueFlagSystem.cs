using System;
using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 对话标志位黑板（对话/任务共用的轻量数据黑板）。
    /// 对话写、任务读、对话再读任务状态，避免模块间直接引用。
    /// 数据存 <see cref="SaveData.dialogue"/> 段，变动即存。
    /// </summary>
    public static class DialogueFlagSystem
    {
        /// <summary>onceOnly 对话读过后写入的标志位前缀。</summary>
        public const string SeenPrefix = "dlg_seen_";

        /// <summary>
        /// 标志位写入通知（任务系统的 flag 目标订阅此事件）。
        /// </summary>
        public static event Action<string> OnFlagSet;

        public static bool Has(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return SaveSystem.Data.dialogue.flags.Contains(key);
        }

        public static void Set(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            List<string> flags = SaveSystem.Data.dialogue.flags;
            if (!flags.Contains(key))
            {
                flags.Add(key);
                SaveSystem.Flush();
                OnFlagSet?.Invoke(key);
            }
        }

        public static void Clear(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (SaveSystem.Data.dialogue.flags.Remove(key))
            {
                SaveSystem.Flush();
            }
        }

        /// <summary>GM 调试用：清空全部对话标志位。</summary>
        public static void ResetAll()
        {
            SaveSystem.Data.dialogue.flags.Clear();
            SaveSystem.Flush();
        }
    }
}
