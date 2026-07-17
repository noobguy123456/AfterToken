using TMPro;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 登录 UI。
    /// </summary>
    [Window(UILayer.UI, location: "LoginUI")]
    public class LoginUI : UIWindow
    {
        private TMP_InputField _inputAccount;
        private TMP_InputField _inputPassword;
        private Button _loginButton;

        protected override void ScriptGenerator()
        {
            _inputAccount = FindChildComponent<TMP_InputField>("m_inputAccount");
            _inputPassword = FindChildComponent<TMP_InputField>("m_inputPassword");
            _loginButton = FindChildComponent<Button>("m_btnLogin");
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            BindEvents();
        }

        private void BindEvents()
        {
            _loginButton?.onClick.RemoveAllListeners();
            _loginButton?.onClick.AddListener(OnLoginClick);
        }

        private void OnLoginClick()
        {
            var account = _inputAccount?.text ?? string.Empty;
            var password = _inputPassword?.text ?? string.Empty;

            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(password))
            {
                Log.Warning("[LoginUI] 账号或密码为空");
                return;
            }

            Log.Info($"[LoginUI] 登录请求: {account}");
            // TODO: 接入登录服务器验证。
        }

        protected override void OnDestroy()
        {
            _loginButton?.onClick.RemoveAllListeners();
            base.OnDestroy();
        }
    }
}
