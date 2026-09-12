using TMPro;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 玩家死亡弹窗。
    /// </summary>
    [Window(UILayer.Top, location: "PlayerDeathUI", fullScreen: true)]
    public class PlayerDeathUI : UIWindow
    {
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _messageText;
        private Button _restartButton;
        private Button _returnButton;

        protected override void ScriptGenerator()
        {
            _titleText = FindChildComponent<TextMeshProUGUI>("m_text_Title");
            _messageText = FindChildComponent<TextMeshProUGUI>("m_text_Message");
            _restartButton = FindChildComponent<Button>("m_btn_Restart");
            _returnButton = FindChildComponent<Button>("m_btn_ReturnToLobby");
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            BindTexts();
            BindEvents();
        }

        private void BindTexts()
        {
            if (_titleText != null) Loc.Bind(_titleText, "ui.death.title");
            if (_messageText != null) Loc.Bind(_messageText, "ui.death.subtitle");
            if (_restartButton != null)
            {
                var restartText = _restartButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (restartText != null) Loc.Bind(restartText, "ui.death.restart");
            }
            if (_returnButton != null)
            {
                var returnText = _returnButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (returnText != null) Loc.Bind(returnText, "ui.death.back_to_base");
            }
        }

        private void BindEvents()
        {
            if (_userDatas != null && _userDatas.Length > 0 && _userDatas[0] is PlayerDeathHandler handler)
            {
                _restartButton?.onClick.RemoveAllListeners();
                _restartButton?.onClick.AddListener(handler.ConfirmRestart);
                _returnButton?.onClick.RemoveAllListeners();
                _returnButton?.onClick.AddListener(handler.ConfirmReturnToLobby);
            }
            else
            {
                Log.Warning("[PlayerDeathUI] 未传入 PlayerDeathHandler，按钮事件未绑定。");
            }
        }

        protected override void OnDestroy()
        {
            _restartButton?.onClick.RemoveAllListeners();
            _returnButton?.onClick.RemoveAllListeners();
            base.OnDestroy();
        }
    }
}
