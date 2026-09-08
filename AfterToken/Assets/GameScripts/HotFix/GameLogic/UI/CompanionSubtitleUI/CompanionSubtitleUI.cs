using TMPro;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// AI 队友字幕条：屏幕底部居中，显示"呼号: 台词"，4s 自动淡出，新台词顶掉旧的。
    /// 纯展示、不交互、不挡射线、不暂停游戏；战斗与经营场景均常驻打开。
    /// 字幕内容来源对 UI 透明（本地 bark / LLM 生成统一走 ICompanionEvent.OnCompanionSay）。
    /// </summary>
    [Window(UILayer.UI, location: "CompanionSubtitleUI", fullScreen: false)]
    public class CompanionSubtitleUI : UIWindow
    {
        /// <summary>HUD 不暂停游戏。</summary>
        public override float TimeScaleWhenVisible => InspectorTimeScale ?? 1f;

        /// <summary>台词显示时长（秒）。</summary>
        private const float SHOW_DURATION = 4f;

        private RectTransform _panel;
        private TextMeshProUGUI _line;
        private CanvasGroup _canvasGroup;
        private float _hideTimer;

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            _panel = FindChildComponent<RectTransform>("m_rect_Subtitle");
            _line = FindChildComponent<TextMeshProUGUI>("m_rect_Subtitle/m_text_Line");
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            _canvasGroup = _panel != null ? _panel.GetComponent<CanvasGroup>() : null;
            SetVisible(false);
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent<string, string>(ICompanionEvent_Event.OnCompanionSay, OnCompanionSay);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (_hideTimer > 0f)
            {
                _hideTimer -= Time.deltaTime;
                if (_hideTimer <= 0f)
                {
                    SetVisible(false);
                }
            }
        }

        private void OnCompanionSay(string speaker, string text)
        {
            if (_line == null) return;
            _line.text = $"<color=#FFD76B>{speaker}</color>: {text}";
            _hideTimer = SHOW_DURATION;
            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            if (_panel != null && _panel.gameObject.activeSelf != visible)
            {
                _panel.gameObject.SetActive(visible);
            }
            if (!visible)
            {
                _hideTimer = 0f;
            }
        }
    }
}
