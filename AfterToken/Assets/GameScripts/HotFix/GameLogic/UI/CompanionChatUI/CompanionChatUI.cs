using TMPro;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// AI 队友自由对话输入条（M6）：T 键打开（可在设置改绑），底部居中、字幕上方。
    /// Enter 发送给 <see cref="AI.Llm.CompanionBrain"/> 的自由对话通道，ESC 关闭；不暂停游戏。
    /// 打开期间 InputSystem / SimulationInputSystem / SimulationPlayerController / BuildingPlacementSystem
    /// 读取 <see cref="IsOpen"/> 抑制玩法输入（打字不走路、不触发功能键）。
    /// 回复统一走 ICompanionEvent.OnCompanionSay 由 CompanionSubtitleUI 上字幕，本窗口只做输入。
    /// </summary>
    [Window(UILayer.Top, location: "CompanionChatUI", fullScreen: false)]
    public class CompanionChatUI : UIWindow
    {
        /// <summary>聊天不暂停游戏。</summary>
        public override float TimeScaleWhenVisible => InspectorTimeScale ?? 1f;

        /// <summary>聊天输入框是否打开（玩法输入抑制用，静态直读避免每帧查 UIModule）。</summary>
        public static bool IsOpen { get; private set; }

        private TMP_InputField _input;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _statusText;
        private Button _infoButton;

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            _titleText = FindChildComponent<TextMeshProUGUI>("m_rect_ChatBar/m_text_Title");
            _statusText = FindChildComponent<TextMeshProUGUI>("m_rect_ChatBar/m_text_Status");
            _input = FindChildComponent<TMP_InputField>("m_rect_ChatBar/m_input_Message");
            _infoButton = FindChildComponent<Button>("m_rect_ChatBar/m_btn_Info");
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            IsOpen = true;
            CursorManager.Instance?.ShowCursor();

            var brain = CompanionSystem.Instance != null ? CompanionSystem.Instance.Brain : null;
            if (brain != null)
            {
                brain.ChatUIOpen = true;
            }
            if (_titleText != null)
            {
                string name = brain != null ? brain.CompanionName : CompanionEntity.CompanionName;
                _titleText.text = Loc.Get("ui.chat.title", name.ToUpperInvariant());
            }
            if (_statusText != null)
            {
                _statusText.text = Loc.Get("ui.chat.transmitting");
                _statusText.gameObject.SetActive(false);
            }
            if (_input != null)
            {
                var placeholder = _input.placeholder as TextMeshProUGUI;
                if (placeholder != null)
                {
                    placeholder.text = Loc.Get("ui.chat.hint");
                }
                // 清空残留文本：上次关窗前若有未发送字符（或关窗键的字符漏进输入框）不带入下次
                _input.text = string.Empty;
                _input.onSubmit.AddListener(OnSubmit);
                _input.ActivateInputField();
            }

            // Info 按钮：打开队友信息面板（好感/记忆/赠礼）
            if (_infoButton != null)
            {
                var infoLabel = _infoButton.GetComponentInChildren<TextMeshProUGUI>();
                Loc.Bind(infoLabel, "ui.interact.companion_info");
                _infoButton.onClick.AddListener(() => GameModule.UI.ShowUIAsync<CompanionInfoUI>());
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            var brain = CompanionSystem.Instance != null ? CompanionSystem.Instance.Brain : null;
            if (_statusText != null)
            {
                bool waiting = brain != null && brain.ChatInFlight;
                if (_statusText.gameObject.activeSelf != waiting)
                {
                    _statusText.gameObject.SetActive(waiting);
                }
            }

            // 再按一次聊天绑定键关闭窗口（回车键除外：回车是发送，由 OnSubmit 空文本关闭兜底）
            var chatKey = KeyBindingSetting.GetKey(KeyBindAction.CompanionChat);
            if (chatKey != KeyCode.Return && chatKey != KeyCode.KeypadEnter && Input.GetKeyDown(chatKey))
            {
                GameModule.UI.CloseUI<CompanionChatUI>();
                return;
            }

            // 保持输入框聚焦：发送后 TMP 会失焦，点到别处也要能继续打字
            if (_input != null && !_input.isFocused)
            {
                _input.ActivateInputField();
            }
        }

        private void OnSubmit(string text)
        {
            // 空文本提交 = 关闭窗口（聊天键绑回车时，空 Enter 即关窗）
            if (string.IsNullOrWhiteSpace(text))
            {
                GameModule.UI.CloseUI<CompanionChatUI>();
                return;
            }

            var brain = CompanionSystem.Instance != null ? CompanionSystem.Instance.Brain : null;
            if (brain != null)
            {
                brain.RequestChatReply(text);
            }
            if (_input != null)
            {
                _input.text = string.Empty;
                _input.ActivateInputField();
            }
        }

        protected override void OnDestroy()
        {
            IsOpen = false;
            var brain = CompanionSystem.Instance != null ? CompanionSystem.Instance.Brain : null;
            if (brain != null)
            {
                brain.ChatUIOpen = false;
            }
            CursorManager.Instance?.HideCursor();
            base.OnDestroy();
        }
    }
}
