using GameConfig.cfg;
using TMPro;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 小纸条阅读面板（叙事 UI）。
    /// 约 1/3 屏幕大小（640x360），显示标题 + 正文。
    /// 不暂停游戏；Esc/E/关闭按钮或走出触发区关闭。
    /// </summary>
    [Window(UILayer.Top, location: "NoteUI", fullScreen: false)]
    public class NoteUI : UIWindow
    {
        /// <summary>
        /// 阅读不暂停游戏（若 UI Prefab 上挂了 UIWindowTimeScale，Inspector 值可覆盖此处默认值）。
        /// </summary>
        public override float TimeScaleWhenVisible => InspectorTimeScale ?? 1f;

        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _contentText;
        private Button _closeButton;

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            _titleText = FindChildComponent<TextMeshProUGUI>("m_img_Background/m_text_Title");
            _contentText = FindChildComponent<TextMeshProUGUI>("m_img_Background/m_text_Content");
            _closeButton = FindChildComponent<Button>("m_img_Background/m_btn_Close");
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            CursorManager.Instance?.ShowCursor();
            CrosshairUpdater.Instance?.SetVisible(false);
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            var cfg = UserDatas != null && UserDatas.Length > 0 ? UserDatas[0] as Note : null;
            RefreshContent(cfg);
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(() => GameModule.UI.CloseUI<NoteUI>());
            }
        }

        protected override void OnDestroy()
        {
            CrosshairUpdater.Instance?.SetVisible(true);
            CursorManager.Instance?.HideCursor();
            base.OnDestroy();
        }

        private void RefreshContent(Note cfg)
        {
            if (_titleText != null)
            {
                _titleText.text = cfg != null ? cfg.Title : "Note";
            }

            if (_contentText != null)
            {
                _contentText.text = cfg != null ? cfg.Content : string.Empty;
            }
        }
    }
}
