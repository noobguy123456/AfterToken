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
            if (_titleText != null) _titleText.text = "玩家死亡";
            if (_messageText != null) _messageText.text = "你已被击败，请选择下一步操作";
            if (_restartButton != null)
            {
                var restartText = _restartButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (restartText != null) restartText.text = "重新开始";
            }
            if (_returnButton != null)
            {
                var returnText = _returnButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (returnText != null) returnText.text = "返回关卡选择";
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
