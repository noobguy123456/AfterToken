using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 多语言静态门面。UI 代码统一经此取词/绑定。
    /// Bind 会把文本注册进全局列表，语言切换时自动刷新（热更类型无法序列化到 prefab，故走运行时代码绑定）。
    /// </summary>
    public static class Loc
    {
        private class Binding
        {
            public TMP_Text Text;
            public string Key;
        }

        private static readonly List<Binding> _bindings = new List<Binding>();
        private static bool _eventHooked;

        /// <summary>
        /// 取词。
        /// </summary>
        public static string Get(string key)
        {
            return LocalizationSystem.Instance.GetText(key);
        }

        /// <summary>
        /// 取词并参数化（词条内用 {0} {1} 占位）。
        /// </summary>
        public static string Get(string key, params object[] args)
        {
            return LocalizationSystem.Instance.GetText(key, args);
        }

        /// <summary>
        /// 绑定 TMP 文本到词条：立即设置，并在语言切换时自动刷新。
        /// </summary>
        public static void Bind(TMP_Text text, string key)
        {
            if (text == null)
            {
                return;
            }

            EnsureEventHooked();
            text.text = Get(key);
            _bindings.Add(new Binding { Text = text, Key = key });
        }

        private static void EnsureEventHooked()
        {
            if (_eventHooked)
            {
                return;
            }
            _eventHooked = true;
            LocalizationSystem.Instance.OnLanguageChanged += RefreshAll;
        }

        private static void RefreshAll()
        {
            // 顺带清理已销毁的文本（Unity 假空）。
            for (int i = _bindings.Count - 1; i >= 0; i--)
            {
                var b = _bindings[i];
                if (b.Text == null)
                {
                    _bindings.RemoveAt(i);
                    continue;
                }
                b.Text.text = Get(b.Key);
            }
        }
    }
}
