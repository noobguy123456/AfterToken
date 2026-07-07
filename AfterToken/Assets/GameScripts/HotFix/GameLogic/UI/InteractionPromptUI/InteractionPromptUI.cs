using TMPro;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 交互提示 UI。
    /// 显示在屏幕中央偏下，用于提示玩家按交互键。
    /// </summary>
    [Window(UILayer.Top, location: "InteractionPromptUI")]
    public class InteractionPromptUI : UIWindow
    {
        private TextMeshProUGUI _promptText;

        protected override void OnCreate()
        {
            base.OnCreate();
            BuildUI();
        }

        private void BuildUI()
        {
            var rt = rectTransform;
            if (rt == null) return;

            var go = new GameObject("PromptText");
            go.transform.SetParent(rt, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, -120);
            rect.sizeDelta = new Vector2(600, 60);

            _promptText = go.AddComponent<TextMeshProUGUI>();
            _promptText.alignment = TextAlignmentOptions.Center;
            _promptText.fontSize = 28;
            _promptText.color = Color.white;
            _promptText.text = string.Empty;
        }

        /// <summary>
        /// 设置提示文本。
        /// </summary>
        public void SetPrompt(string text)
        {
            if (_promptText != null)
            {
                _promptText.text = text;
            }
        }
    }
}
