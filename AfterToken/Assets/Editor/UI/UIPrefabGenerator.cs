using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace GameLogic.Editor
{
    /// <summary>
    /// 单个 UI Prefab 生成工具。
    /// 用于快速创建符合项目规范的 UI 脚本和 Prefab，不影响其他资源。
    /// </summary>
    public class UIPrefabGenerator : EditorWindow
    {
        private enum UILayerOption
        {
            Bottom,
            UI,
            Top,
            Tips,
            System,
        }

        private string _uiName = "NewUI";
        private UILayerOption _layer = UILayerOption.UI;
        private bool _fullScreen = true;
        private bool _useCanvasScaler = false;
        private Vector2 _referenceResolution = new Vector2(1920f, 1080f);
        private float _matchWidthOrHeight = 0.5f;

        [MenuItem("Tools/UI/Create UI Prefab")]
        private static void OpenWindow()
        {
            var window = GetWindow<UIPrefabGenerator>("Create UI Prefab");
            window.minSize = new Vector2(360f, 300f);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("生成单个 UI 的脚本和 Prefab", EditorStyles.boldLabel);
            GUILayout.Space(10);

            _uiName = EditorGUILayout.TextField("UI 名称", _uiName);
            _layer = (UILayerOption)EditorGUILayout.EnumPopup("UILayer", _layer);
            _fullScreen = EditorGUILayout.Toggle("是否全屏", _fullScreen);
            _useCanvasScaler = EditorGUILayout.Toggle("独立 CanvasScaler", _useCanvasScaler);

            if (_useCanvasScaler)
            {
                EditorGUI.indentLevel++;
                _referenceResolution = EditorGUILayout.Vector2Field("参考分辨率", _referenceResolution);
                _matchWidthOrHeight = EditorGUILayout.Slider("Match", _matchWidthOrHeight, 0f, 1f);
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(20);

            GUI.enabled = !string.IsNullOrWhiteSpace(_uiName);
            if (GUILayout.Button("生成", GUILayout.Height(32)))
            {
                Generate();
            }

            GUI.enabled = true;

            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "生成后会创建：\n" +
                "1. Assets/GameScripts/HotFix/GameLogic/UI/{Name}/{Name}.cs\n" +
                "2. Assets/AssetRaw/UI/{Name}/{Name}.prefab\n\n" +
                "人类负责在 Prefab Mode 调整位置和美术；\n" +
                "不要直接修改 ScriptGenerator() 里的路径，改路径时请重新提需求给 AI。",
                MessageType.Info);
        }

        private void Generate()
        {
            var name = _uiName.Trim();
            if (!ValidateName(name))
            {
                EditorUtility.DisplayDialog("错误", "UI 名称只能以字母开头，且只能包含字母、数字和下划线。", "确定");
                return;
            }

            var scriptDir = $"Assets/GameScripts/HotFix/GameLogic/UI/{name}";
            var prefabDir = $"Assets/AssetRaw/UI/{name}";
            var scriptPath = $"{scriptDir}/{name}.cs";
            var prefabPath = $"{prefabDir}/{name}.prefab";

            if (File.Exists(scriptPath))
            {
                EditorUtility.DisplayDialog("错误", $"脚本已存在：{scriptPath}\n请手动删除后再生成，或改用增量更新。", "确定");
                return;
            }

            if (File.Exists(prefabPath))
            {
                EditorUtility.DisplayDialog("错误", $"Prefab 已存在：{prefabPath}\n请手动删除后再生成，或改用增量更新。", "确定");
                return;
            }

            EnsureDirectory(scriptDir);
            EnsureDirectory(prefabDir);

            try
            {
                GenerateScript(name, scriptPath);
                GeneratePrefab(name, prefabPath);

                AssetDatabase.Refresh();

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    EditorGUIUtility.PingObject(prefab);
                }

                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                if (script != null)
                {
                    AssetDatabase.OpenAsset(script);
                }

                Debug.Log($"[UIPrefabGenerator] 已生成 UI: {name}\n  {scriptPath}\n  {prefabPath}");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("生成失败", e.Message, "确定");
                Debug.LogException(e);
            }
        }

        private static bool ValidateName(string name)
        {
            return !string.IsNullOrEmpty(name) && Regex.IsMatch(name, "^[A-Za-z][A-Za-z0-9_]*$");
        }

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }

        private void GenerateScript(string name, string path)
        {
            var layer = _layer.ToString();
            var fullScreen = _fullScreen ? ", fullScreen: true" : "";
            var contentNode = "m_rect_Content";

            var code =
$"using UnityEngine;\n" +
$"using UnityEngine.UI;\n" +
$"using TEngine;\n" +
$"\n" +
$"namespace GameLogic\n" +
$"{{\n" +
$"    /// <summary>\n" +
$"    /// {name}。\n" +
$"    /// </summary>\n" +
$"    [Window(UILayer.{layer}, location: \"{name}\"{fullScreen})]\n" +
$"    public class {name} : UIWindow\n" +
$"    {{\n" +
$"        private Text _textTitle;\n" +
$"        private Button _btnClose;\n" +
$"\n" +
$"        #region 脚本工具生成的代码\n" +
$"        protected override void ScriptGenerator()\n" +
$"        {{\n" +
$"            _textTitle = FindChildComponent<Text>(\"{contentNode}/m_text_Title\");\n" +
$"            _btnClose = FindChildComponent<Button>(\"{contentNode}/m_btn_Close\");\n" +
$"        }}\n" +
$"        #endregion\n" +
$"\n" +
$"        protected override void OnCreate()\n" +
$"        {{\n" +
$"            base.OnCreate();\n" +
$"            FixFullScreenCanvas();\n" +
$"            BindEvents();\n" +
$"            RefreshAll();\n" +
$"        }}\n" +
$"\n" +
$"        private void BindEvents()\n" +
$"        {{\n" +
$"            _btnClose?.onClick.RemoveAllListeners();\n" +
$"            _btnClose?.onClick.AddListener(() =>\n" +
$"            {{\n" +
$"                // TODO: 关闭或返回逻辑\n" +
$"                UIModule.Instance.CloseUI<{name}>();\n" +
$"            }});\n" +
$"        }}\n" +
$"\n" +
$"        private void RefreshAll()\n" +
$"        {{\n" +
$"            if (_textTitle != null)\n" +
$"            {{\n" +
$"                _textTitle.text = \"{name}\";\n" +
$"            }}\n" +
$"        }}\n" +
$"    }}\n" +
$"}}\n";

            File.WriteAllText(path, code);
        }

        private void GeneratePrefab(string name, string path)
        {
            var go = new GameObject(name);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            go.AddComponent<GraphicRaycaster>();

            if (_useCanvasScaler)
            {
                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = _referenceResolution;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = _matchWidthOrHeight;
            }

            var rootRt = go.GetComponent<RectTransform>();
            if (rootRt != null)
            {
                rootRt.localScale = Vector3.one;
                rootRt.anchorMin = Vector2.zero;
                rootRt.anchorMax = Vector2.one;
                rootRt.offsetMin = Vector2.zero;
                rootRt.offsetMax = Vector2.zero;
                rootRt.sizeDelta = Vector2.zero;
                rootRt.pivot = new Vector2(0.5f, 0.5f);
            }

            // 内容根节点
            var content = new GameObject("m_rect_Content");
            content.transform.SetParent(go.transform, false);
            var contentRt = content.AddComponent<RectTransform>();
            contentRt.localScale = Vector3.one;
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;
            contentRt.pivot = new Vector2(0.5f, 0.5f);

            // 标题示例
            var title = new GameObject("m_text_Title");
            title.transform.SetParent(content.transform, false);
            var titleRt = title.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -60f);
            titleRt.sizeDelta = new Vector2(400f, 60f);

            var titleText = title.AddComponent<Text>();
            titleText.text = name;
            titleText.font = GetUIFont();
            titleText.fontSize = 36;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;

            // 关闭按钮示例
            var close = new GameObject("m_btn_Close");
            close.transform.SetParent(content.transform, false);
            var closeRt = close.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-40f, -40f);
            closeRt.sizeDelta = new Vector2(80f, 40f);

            var closeImg = close.AddComponent<Image>();
            closeImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);

            var closeBtn = close.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;

            var closeLabel = new GameObject("Text");
            closeLabel.transform.SetParent(close.transform, false);
            var closeLabelRt = closeLabel.AddComponent<RectTransform>();
            closeLabelRt.anchorMin = Vector2.zero;
            closeLabelRt.anchorMax = Vector2.one;
            closeLabelRt.offsetMin = Vector2.zero;
            closeLabelRt.offsetMax = Vector2.zero;

            var closeLabelText = closeLabel.AddComponent<Text>();
            closeLabelText.text = "X";
            closeLabelText.font = GetUIFont();
            closeLabelText.fontSize = 24;
            closeLabelText.color = Color.white;
            closeLabelText.alignment = TextAnchor.MiddleCenter;

            PrefabUtility.SaveAsPrefabAsset(go, path);
            DestroyImmediate(go);
        }

        private static Font GetUIFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }
    }
}
