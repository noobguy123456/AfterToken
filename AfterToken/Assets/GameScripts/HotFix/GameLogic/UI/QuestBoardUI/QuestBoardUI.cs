using TMPro;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 任务板面板（基地）：列出任务板（giverNpc=0）发布的可接任务。
    /// 打开时机：基地走近任务板按 E（QuestBoardSystem）；ESC 关窗链已注册。
    /// 点击列表项弹 <see cref="QuestAcceptConfirmUI"/> 确认接取；接取成功后列表即时刷新移除该任务。
    /// 不暂停游戏；光标/准星按 CursorManager 配对规则（同 QuestLogUI）。
    /// </summary>
    [Window(UILayer.Top, location: "QuestBoardUI", fullScreen: false)]
    public class QuestBoardUI : UIWindow
    {
        /// <summary>看任务板不暂停游戏。</summary>
        public override float TimeScaleWhenVisible => InspectorTimeScale ?? 1f;

        /// <summary>任务板发布者的 giverNpc 约定值（quest.csv 表头注释）。</summary>
        private const int BoardGiverNpcId = 0;

        private RectTransform _questListRoot;
        private RectTransform _questItemTemplate;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _emptyText;
        private Button _closeButton;

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            _questListRoot = FindChildComponent<RectTransform>("m_img_Background/m_rect_QuestList");
            _questItemTemplate = FindChildComponent<RectTransform>("m_img_Background/m_rect_QuestList/m_btn_QuestTemplate");
            _titleText = FindChildComponent<TextMeshProUGUI>("m_img_Background/m_text_Title");
            _emptyText = FindChildComponent<TextMeshProUGUI>("m_img_Background/m_text_Empty");
            _closeButton = FindChildComponent<Button>("m_img_Background/m_btn_Close");
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            CursorManager.Instance?.ShowCursor();
            CrosshairUpdater.Instance?.SetVisible(false);
            Loc.Bind(_titleText, "ui.questboard.title");
            RebuildList();
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent<int>(IQuestEvent_Event.OnQuestAccepted, _ => RebuildList());

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(() => GameModule.UI.CloseUI<QuestBoardUI>());
            }
        }

        protected override void OnDestroy()
        {
            CrosshairUpdater.Instance?.SetVisible(true);
            CursorManager.Instance?.HideCursor();
            base.OnDestroy();
        }

        private void RebuildList()
        {
            if (_questListRoot == null || _questItemTemplate == null) return;

            for (int i = _questListRoot.childCount - 1; i >= 0; i--)
            {
                var child = _questListRoot.GetChild(i);
                if (child == _questItemTemplate) continue;
                Object.Destroy(child.gameObject);
            }

            int shown = 0;
            var quests = QuestConfigMgr.Instance.GetQuestsByGiver(BoardGiverNpcId);
            for (int i = 0; i < quests.Count; i++)
            {
                var cfg = quests[i];
                if (!QuestSystem.CanAccept(cfg.Id)) continue;

                var item = Object.Instantiate(_questItemTemplate.gameObject, _questListRoot);
                item.name = $"m_btn_Quest_{cfg.Id}";
                item.SetActive(true);

                var text = item.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = $"{cfg.Name}\n<size=80%><color=#BBBBBB>{cfg.Desc}</color></size>";
                }

                var btn = item.GetComponent<Button>();
                if (btn != null)
                {
                    int questId = cfg.Id;
                    btn.onClick.AddListener(() => GameModule.UI.ShowUIAsync<QuestAcceptConfirmUI>(questId));
                }

                shown++;
            }

            // 空列表提示（词条）
            if (_emptyText != null)
            {
                _emptyText.gameObject.SetActive(shown == 0);
                if (shown == 0)
                {
                    _emptyText.text = Loc.Get("ui.questboard.empty");
                }
            }
        }
    }
}
