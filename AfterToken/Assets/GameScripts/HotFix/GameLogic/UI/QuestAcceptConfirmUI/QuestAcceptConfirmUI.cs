using TMPro;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 任务接取确认弹窗（叙事 UI）。
    /// 对话动作 quest:accept:id 不再直接接取，改为弹此窗：Confirm 接取 / Cancel 放弃。
    /// Enter 确认、Esc 取消（弹窗期间 DialogueSystem 不响应推进键，见 DialogueSystem.Update 守卫）；
    /// 对话被结束（含 Esc 关对话、走出触发区）时本窗自动按取消处理。
    /// 不暂停游戏；光标/准星按 CursorManager 配对规则（同 NoteUI）。
    /// </summary>
    [Window(UILayer.Top, location: "QuestAcceptConfirmUI", fullScreen: false)]
    public class QuestAcceptConfirmUI : UIWindow
    {
        /// <summary>确认弹窗不暂停游戏。</summary>
        public override float TimeScaleWhenVisible => InspectorTimeScale ?? 1f;

        private TextMeshProUGUI _questNameText;
        private TextMeshProUGUI _questDescText;
        private Button _confirmButton;
        private Button _cancelButton;

        private int _questId;
        private bool _finished;

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            _questNameText = FindChildComponent<TextMeshProUGUI>("m_img_Panel/m_text_QuestName");
            _questDescText = FindChildComponent<TextMeshProUGUI>("m_img_Panel/m_text_QuestDesc");
            _confirmButton = FindChildComponent<Button>("m_img_Panel/m_btn_Confirm");
            _cancelButton = FindChildComponent<Button>("m_img_Panel/m_btn_Cancel");
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            CursorManager.Instance?.ShowCursor();
            CrosshairUpdater.Instance?.SetVisible(false);
            _finished = false;
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            _questId = UserDatas != null && UserDatas.Length > 0 && UserDatas[0] is int id ? id : 0;
            var cfg = _questId != 0 ? QuestConfigMgr.Instance.Get(_questId) : null;
            if (_questNameText != null)
            {
                _questNameText.text = cfg != null ? cfg.Name : $"Quest #{_questId}";
            }
            if (_questDescText != null)
            {
                _questDescText.text = cfg != null ? cfg.Desc : string.Empty;
            }
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveAllListeners();
                _confirmButton.onClick.AddListener(Confirm);
            }
            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveAllListeners();
                _cancelButton.onClick.AddListener(Cancel);
            }
            // 对话结束（Esc 关对话/走出触发区）时按取消处理，避免弹窗游离在对话外
            AddUIEvent<int>(IDialogueEvent_Event.OnDialogueEnded, _ => Cancel());
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Confirm();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cancel();
            }
        }

        protected override void OnDestroy()
        {
            CrosshairUpdater.Instance?.SetVisible(true);
            CursorManager.Instance?.HideCursor();
            base.OnDestroy();
        }

        private void Confirm()
        {
            if (_finished) return;
            _finished = true;
            if (_questId != 0 && !QuestSystem.Accept(_questId))
            {
                Log.Warning($"[QuestAcceptConfirmUI] 任务接取失败（条件不满足或已接取）: {_questId}");
            }
            GameModule.UI.CloseUI<QuestAcceptConfirmUI>();
        }

        private void Cancel()
        {
            if (_finished) return;
            _finished = true;
            GameModule.UI.CloseUI<QuestAcceptConfirmUI>();
        }
    }
}
