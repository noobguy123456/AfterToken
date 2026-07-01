using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 设置面板。
    /// 提供准星灵敏度等游戏内可调选项。
    /// </summary>
    /// <remarks>
    /// 当前为代码动态创建子节点的临时实现，后续应按 UI Prefab 工作流
    /// 在 Prefab Mode 中配置背景、标题、滑块、按钮等节点，并在 ScriptGenerator 中绑定。
    /// </remarks>
    [Window(UILayer.Top, "SettingsUI", false)]
    public class SettingsUI : UIWindow
    {
        /// <summary>
        /// 设置面板打开时暂停游戏进程（不影响声音）。
        /// 若 UI Prefab 上挂了 UIWindowTimeScale，Inspector 值可覆盖此处默认值。
        /// </summary>
        public override float TimeScaleWhenVisible => InspectorTimeScale ?? 0f;

        private RectTransform _contentRoot;
        private TextMeshProUGUI _sensitivityValueText;
        private Slider _sensitivitySlider;

        protected override void ScriptGenerator()
        {
            _contentRoot = FindChildComponent<RectTransform>("m_rect_ContentRoot");
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            CursorManager.Instance?.ShowCursor();
            EnsureContentRoot();
            BuildSettingsContent();
            Log.Debug("[SettingsUI] 设置面板已打开");
        }

        protected override void OnDestroy()
        {
            CursorManager.Instance?.HideCursor();
            SensitivitySetting.Save();
            base.OnDestroy();
        }

        private void EnsureContentRoot()
        {
            if (_contentRoot == null)
            {
                var contentGo = new GameObject("m_rect_ContentRoot");
                contentGo.transform.SetParent(rectTransform, false);
                _contentRoot = contentGo.AddComponent<RectTransform>();
                _contentRoot.anchorMin = Vector2.zero;
                _contentRoot.anchorMax = Vector2.one;
                _contentRoot.offsetMin = Vector2.zero;
                _contentRoot.offsetMax = Vector2.zero;
            }
        }

        private void BuildSettingsContent()
        {
            // 背景遮罩
            var bg = new GameObject("m_img_Background", typeof(RectTransform));
            bg.transform.SetParent(_contentRoot, false);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.85f);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // 标题
            CreateText("m_text_Title", "Settings", 48, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(0f, 60f));

            // 灵敏度标签
            CreateText("m_text_SensitivityLabel", "Crosshair Sensitivity", 28, new Vector2(0f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(0f, 40f));

            // 灵敏度滑块
            CreateSensitivitySlider();

            // 灵敏度数值
            _sensitivityValueText = CreateText("m_text_SensitivityValue", $"Sensitivity: {SensitivitySetting.Value:F2}", 24,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(0f, 40f));

            // 关闭按钮
            CreateCloseButton();
        }

        private TextMeshProUGUI CreateText(string name, string content, int fontSize, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_contentRoot, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.font = TMPFontProvider.DefaultFont;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return text;
        }

        private void CreateSensitivitySlider()
        {
            var sliderObj = new GameObject("m_slider_Sensitivity", typeof(RectTransform));
            sliderObj.transform.SetParent(_contentRoot, false);
            _sensitivitySlider = sliderObj.AddComponent<Slider>();
            var sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.25f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.75f, 0.5f);
            sliderRect.anchoredPosition = Vector2.zero;
            sliderRect.sizeDelta = new Vector2(0f, 30f);

            SetupSliderVisuals(_sensitivitySlider);
            _sensitivitySlider.minValue = SensitivitySetting.Min;
            _sensitivitySlider.maxValue = SensitivitySetting.Max;
            _sensitivitySlider.value = SensitivitySetting.Value;
            _sensitivitySlider.wholeNumbers = false;

            _sensitivitySlider.onValueChanged.RemoveAllListeners();
            _sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        private void SetupSliderVisuals(Slider slider)
        {
            var background = new GameObject("Background", typeof(RectTransform));
            background.transform.SetParent(slider.transform, false);
            var bgImage = background.AddComponent<Image>();
            bgImage.color = Color.gray;
            var bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(slider.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(5f, 0f);
            fillAreaRect.offsetMax = new Vector2(-5f, 0f);

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 1f, 0.2f, 1f);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.pivot = new Vector2(0f, 0.5f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(slider.transform, false);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(5f, 0f);
            handleAreaRect.offsetMax = new Vector2(-5f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform));
            handle.transform.SetParent(handleArea.transform, false);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(24f, 36f);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
        }

        private void CreateCloseButton()
        {
            var btnObj = new GameObject("m_btn_Close", typeof(RectTransform));
            btnObj.transform.SetParent(_contentRoot, false);
            var image = btnObj.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            var btn = btnObj.AddComponent<Button>();
            var rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 120f);
            rect.sizeDelta = new Vector2(200f, 60f);

            var textObj = new GameObject("m_text_Close", typeof(RectTransform));
            textObj.transform.SetParent(btnObj.transform, false);
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Close";
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            text.font = TMPFontProvider.DefaultFont;
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => GameModule.UI.CloseUI<SettingsUI>());
        }

        private void OnSensitivityChanged(float value)
        {
            SensitivitySetting.Value = value;
            if (_sensitivityValueText != null)
            {
                _sensitivityValueText.text = $"Sensitivity: {value:F2}";
            }
        }
    }
}
