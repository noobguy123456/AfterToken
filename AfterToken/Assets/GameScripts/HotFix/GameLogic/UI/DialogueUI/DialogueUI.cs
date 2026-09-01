using TMPro;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 对话窗口（叙事 UI）：底部约 1/4 屏对话框（说话人 + 正文打字机 + 继续提示），
    /// 选项时中部弹出纵向按钮列表（数字键 1~4 或点击选择）。
    /// 不暂停游戏；不显示系统光标（E 推进、数字键选择，避免与光标管理打架）。
    /// 只订阅 <see cref="IDialogueEvent"/> 呈现，不知道表结构。
    /// </summary>
    [Window(UILayer.Top, location: "DialogueUI", fullScreen: false)]
    public class DialogueUI : UIWindow
    {
        /// <summary>对话不暂停游戏。</summary>
        public override float TimeScaleWhenVisible => InspectorTimeScale ?? 1f;

        /// <summary>打字机速度（字符/秒）。</summary>
        private const float CharsPerSecond = 45f;

        private const int MaxChoices = 4;

        private readonly GameEventMgr _eventMgr = new GameEventMgr();

        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _contentText;
        private TextMeshProUGUI _hintText;
        private RectTransform _choicesRoot;
        private readonly Button[] _choiceButtons = new Button[MaxChoices];
        private readonly TextMeshProUGUI[] _choiceTexts = new TextMeshProUGUI[MaxChoices];

        private float _visibleChars;

        /// <summary>打字机是否还在输出中（DialogueSystem 用它决定 E 键先补全再推进）。</summary>
        public bool IsTyping { get; private set; }

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            _nameText = FindChildComponent<TextMeshProUGUI>("m_img_Panel/m_text_Name");
            _contentText = FindChildComponent<TextMeshProUGUI>("m_img_Panel/m_text_Content");
            _hintText = FindChildComponent<TextMeshProUGUI>("m_img_Panel/m_text_Hint");
            _choicesRoot = FindChildComponent<RectTransform>("m_img_Panel/m_rect_Choices");
            for (int i = 0; i < MaxChoices; i++)
            {
                _choiceButtons[i] = FindChildComponent<Button>($"m_img_Panel/m_rect_Choices/m_btn_Choice{i}");
                _choiceTexts[i] = FindChildComponent<TextMeshProUGUI>($"m_img_Panel/m_rect_Choices/m_btn_Choice{i}/m_text_Label");
            }
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();

            if (_choicesRoot != null)
            {
                _choicesRoot.gameObject.SetActive(false);
            }
            for (int i = 0; i < MaxChoices; i++)
            {
                int index = i;
                if (_choiceButtons[i] != null)
                {
                    _choiceButtons[i].onClick.RemoveAllListeners();
                    _choiceButtons[i].onClick.AddListener(() => DialogueSystem.Instance?.Choose(index));
                }
            }
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            _eventMgr.AddEvent<int, int>(IDialogueEvent_Event.OnDialogueStarted, OnDialogueStarted);
            _eventMgr.AddEvent<int, string, string>(IDialogueEvent_Event.OnDialogueLine, OnDialogueLine);
            _eventMgr.AddEvent<int, string[]>(IDialogueEvent_Event.OnDialogueChoices, OnDialogueChoices);
        }

        protected override void OnDestroy()
        {
            _eventMgr.Clear();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (IsTyping && _contentText != null)
            {
                _visibleChars += CharsPerSecond * Time.unscaledDeltaTime;
                _contentText.maxVisibleCharacters = Mathf.FloorToInt(_visibleChars);
                if (_contentText.maxVisibleCharacters >= _contentText.textInfo.characterCount)
                {
                    IsTyping = false;
                }
            }
        }

        /// <summary>
        /// 补全当前行（打字中按 E 的两拍交互第一拍）。
        /// </summary>
        public void CompleteLine()
        {
            if (_contentText != null)
            {
                _contentText.maxVisibleCharacters = int.MaxValue;
            }
            IsTyping = false;
        }

        private void OnDialogueStarted(int dialogueId, int npcId)
        {
            if (_nameText != null) _nameText.text = string.Empty;
            if (_contentText != null)
            {
                _contentText.text = string.Empty;
                _contentText.maxVisibleCharacters = int.MaxValue;
            }
            IsTyping = false;
            if (_choicesRoot != null) _choicesRoot.gameObject.SetActive(false);
        }

        private void OnDialogueLine(int nodeId, string speaker, string text)
        {
            if (_nameText != null)
            {
                _nameText.text = speaker ?? string.Empty;
            }
            StartTypewriter(text ?? string.Empty);
        }

        private void OnDialogueChoices(int nodeId, string[] options)
        {
            if (_choicesRoot == null) return;

            _choicesRoot.gameObject.SetActive(true);
            for (int i = 0; i < MaxChoices; i++)
            {
                bool used = options != null && i < options.Length;
                if (_choiceButtons[i] != null)
                {
                    _choiceButtons[i].gameObject.SetActive(used);
                }
                if (used && _choiceTexts[i] != null)
                {
                    // 数字键前缀提示键盘选择
                    _choiceTexts[i].text = $"{i + 1}. {options[i]}";
                }
            }
        }

        private void StartTypewriter(string text)
        {
            if (_choicesRoot != null)
            {
                _choicesRoot.gameObject.SetActive(false);
            }
            if (_contentText == null) return;

            _contentText.text = text;
            // 强制先排版一次，textInfo 才有正确字符数
            _contentText.ForceMeshUpdate();
            _visibleChars = 0f;
            _contentText.maxVisibleCharacters = 0;
            IsTyping = _contentText.textInfo.characterCount > 0;
        }
    }
}
