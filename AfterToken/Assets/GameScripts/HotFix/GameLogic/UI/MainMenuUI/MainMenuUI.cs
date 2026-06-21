using UnityEngine;
using TMPro;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 主菜单 UI。
    /// </summary>
    [Window(UILayer.UI, location: "MainMenuUI", fullScreen: true)]
    public class MainMenuUI : UIWindow
    {
        private TextMeshProUGUI _titleText;
        private Button _startButton;
        private Button _exitButton;

        #region 脚本工具生成的代码
        protected override void ScriptGenerator()
        {
            _titleText = FindChildComponent<TextMeshProUGUI>("m_text_Title");
            _startButton = FindChildComponent<Button>("m_rect_ButtonRoot/m_btn_Start");
            _exitButton = FindChildComponent<Button>("m_rect_ButtonRoot/m_btn_Exit");
        }
        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            Log.Debug($"[MainMenuUI] 节点绑定: Title={_titleText != null}, Start={_startButton != null}, Exit={_exitButton != null}");
            BindEvents();
        }

        private void BindEvents()
        {
            if (_titleText != null)
            {
                _titleText.text = "AfterToken";
            }

            if (_startButton != null)
            {
                _startButton.onClick.RemoveAllListeners();
                _startButton.onClick.AddListener(() => GameApp.ChangeProcedure<ProcedureLobby>());
            }

            if (_exitButton != null)
            {
                _exitButton.onClick.RemoveAllListeners();
                _exitButton.onClick.AddListener(() =>
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                });
            }
        }
    }
}
