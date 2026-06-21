using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 加载过渡 UI。
    /// </summary>
    [Window(UILayer.System, location: "LoadingUI", fullScreen: true)]
    public class LoadingUI : UIWindow
    {
        private Slider _progressSlider;
        private TextMeshProUGUI _progressText;

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            BuildUI();
        }

        private void BuildUI()
        {
            var rt = rectTransform;
            if (rt == null) return;

            // 背景
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(rt, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = Color.black;

            // 进度条
            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(rt, false);
            var sliderRect = sliderGo.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.anchoredPosition = Vector2.zero;
            sliderRect.sizeDelta = new Vector2(600, 40);

            _progressSlider = sliderGo.AddComponent<Slider>();
            _progressSlider.minValue = 0;
            _progressSlider.maxValue = 1;
            _progressSlider.value = 0;

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.6f, 1f, 1f);
            _progressSlider.fillRect = fillRect;
            _progressSlider.targetGraphic = fillImg;

            // 进度文本
            var textGo = new GameObject("ProgressText");
            textGo.transform.SetParent(rt, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(0, -50);
            textRect.sizeDelta = new Vector2(400, 40);
            _progressText = textGo.AddComponent<TextMeshProUGUI>();
            _progressText.text = "Loading... 0%";
            _progressText.font = TMPFontProvider.DefaultFont;
            _progressText.fontSize = 24;
            _progressText.color = Color.white;
            _progressText.alignment = TextAlignmentOptions.Center;
        }

        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (_progressSlider != null)
            {
                _progressSlider.value = progress;
            }
            if (_progressText != null)
            {
                _progressText.text = $"Loading... {progress * 100:F0}%";
            }
        }
    }
}
