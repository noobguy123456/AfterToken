using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace GameLogic.Editor
{
    /// <summary>
    /// 一次性迁移工具：将项目中的 uGUI Text 替换为 TextMeshProUGUI，
    /// 并创建一份默认 TMP 字体资产。
    /// </summary>
    public static class TMPPrefabMigrator
    {
        public const string FontAssetPath = "Assets/AssetRaw/Fonts/MainUIFont.asset";

        private static readonly Dictionary<string, string> ChineseToEnglish = new Dictionary<string, string>
        {
            { "请输入密码", "Enter Password" },
            { "请输入账号", "Enter Account" },
            { "登录", "Login" },
            { "选择关卡", "Select Level" },
            { "关卡名称", "Level Name" },
            { "返回主菜单", "Back to Menu" },
            { "开始游戏", "Start Game" },
            { "退出游戏", "Exit Game" },
            { "空", "Empty" },
        };

        [MenuItem("Tools/Migration/Migrate UI Prefabs to TMP")]
        public static void Migrate()
        {
            var fontAsset = EnsureFontAsset();
            if (fontAsset == null)
            {
                Debug.LogError("[TMPPrefabMigrator] Failed to create TMP font asset.");
                return;
            }

            var prefabPaths = CollectUIPrefabPaths().ToList();
            int convertedCount = 0;
            foreach (var path in prefabPaths)
            {
                if (ConvertPrefab(path, fontAsset))
                {
                    convertedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[TMPPrefabMigrator] Migrated {convertedCount} prefabs to TMP. Font asset: {FontAssetPath}");
        }

        private static TMP_FontAsset EnsureFontAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null) return existing;

            string dir = Path.GetDirectoryName(FontAssetPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(dir))
            {
                string parent = Path.GetDirectoryName(dir).Replace('\\', '/');
                string folderName = Path.GetFileName(dir);
                AssetDatabase.CreateFolder(parent, folderName);
            }

            TMP_FontAsset fontAsset = null;

            // 优先使用系统 Arial 字体创建动态字体资产
            Font sourceFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
            if (sourceFont != null)
            {
                fontAsset = TMP_FontAsset.CreateFontAsset(
                    sourceFont, 90, 9,
                    GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic, true);
            }

            // 兜底：从已导入的 TMP 默认字体复制
            if (fontAsset == null)
            {
                var defaultFont = TMP_Settings.defaultFontAsset;
                if (defaultFont != null)
                {
                    var cloned = Object.Instantiate(defaultFont);
                    cloned.name = "MainUIFont";
                    AssetDatabase.CreateAsset(cloned, FontAssetPath);
                    return cloned;
                }
            }

            if (fontAsset != null)
            {
                fontAsset.name = "MainUIFont";
                AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            }

            return fontAsset;
        }

        private static IEnumerable<string> CollectUIPrefabPaths()
        {
            string uiRoot = "Assets/AssetRaw/UI";
            if (AssetDatabase.IsValidFolder(uiRoot))
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { uiRoot }))
                {
                    yield return AssetDatabase.GUIDToAssetPath(guid);
                }
            }

            string logUiPath = "Assets/GameScripts/HotFix/GameLogic/Module/UIModule/Resources/LogUI.prefab";
            if (File.Exists(logUiPath))
            {
                yield return logUiPath;
            }
        }

        private static bool ConvertPrefab(string path, TMP_FontAsset fontAsset)
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(path);
            if (prefabRoot == null)
            {
                Debug.LogWarning($"[TMPPrefabMigrator] Failed to load prefab: {path}");
                return false;
            }

            bool modified = false;
            var texts = prefabRoot.GetComponentsInChildren<Text>(true);
            foreach (var text in texts)
            {
                var go = text.gameObject;
                var oldText = text.text;
                var fontSize = text.fontSize;
                var color = text.color;
                var alignment = text.alignment;
                var raycastTarget = text.raycastTarget;

                Object.DestroyImmediate(text, true);
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = TranslateText(oldText);
                tmp.font = fontAsset;
                tmp.fontSize = fontSize;
                tmp.color = color;
                tmp.alignment = ConvertAlignment(alignment);
                tmp.raycastTarget = raycastTarget;

                modified = true;
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return modified;
        }

        private static string TranslateText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (ChineseToEnglish.TryGetValue(text, out var english))
            {
                return english;
            }
            return text;
        }

        private static TextAlignmentOptions ConvertAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }
    }
}
