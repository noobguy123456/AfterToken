using System.Collections.Generic;
using System.Text;
using GameConfig.cfg;
using TMPro;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 任务日志面板（基地）。
    /// 左列表右详情：列表按状态分组（Ready / Active / Available / Done），详情显示目标 x/y 进度与奖励预览。
    /// 打开时机：基地按 J 键（SimulationInputSystem）；ESC 关窗链已注册。
    /// 不暂停游戏；光标/准星按 CursorManager 配对规则（同 NoteUI）。
    /// 设计文档：docs/Proposal/narrative/quest-system.md。
    /// </summary>
    [Window(UILayer.Top, location: "QuestLogUI", fullScreen: false)]
    public class QuestLogUI : UIWindow
    {
        /// <summary>
        /// 看任务日志不暂停游戏（若 UI Prefab 上挂了 UIWindowTimeScale，Inspector 值可覆盖此处默认值）。
        /// </summary>
        public override float TimeScaleWhenVisible => InspectorTimeScale ?? 1f;

        private RectTransform _questListRoot;
        private RectTransform _questItemTemplate;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _descText;
        private TextMeshProUGUI _objectivesText;
        private TextMeshProUGUI _rewardsText;
        private Button _closeButton;

        private int _selectedQuestId;

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            _questListRoot = FindChildComponent<RectTransform>("m_img_Background/m_rect_QuestList");
            _questItemTemplate = FindChildComponent<RectTransform>("m_img_Background/m_rect_QuestList/m_btn_QuestTemplate");
            _nameText = FindChildComponent<TextMeshProUGUI>("m_img_Background/m_rect_Detail/m_text_QuestName");
            _descText = FindChildComponent<TextMeshProUGUI>("m_img_Background/m_rect_Detail/m_text_QuestDesc");
            _objectivesText = FindChildComponent<TextMeshProUGUI>("m_img_Background/m_rect_Detail/m_text_Objectives");
            _rewardsText = FindChildComponent<TextMeshProUGUI>("m_img_Background/m_rect_Detail/m_text_Rewards");
            _closeButton = FindChildComponent<Button>("m_img_Background/m_btn_Close");
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            CursorManager.Instance?.ShowCursor();
            CrosshairUpdater.Instance?.SetVisible(false);
            RebuildList();
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent<int>(IQuestEvent_Event.OnQuestAccepted, _ => RebuildList());
            AddUIEvent<int>(IQuestEvent_Event.OnQuestReadyToTurnIn, _ => RebuildList());
            AddUIEvent<int>(IQuestEvent_Event.OnQuestCompleted, _ => RebuildList());
            AddUIEvent<int, int, int, int>(IQuestEvent_Event.OnObjectiveProgress, (questId, _, _, _) =>
            {
                if (questId == _selectedQuestId)
                {
                    ShowDetail(questId);
                }
            });

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(() => GameModule.UI.CloseUI<QuestLogUI>());
            }
        }

        protected override void OnDestroy()
        {
            CrosshairUpdater.Instance?.SetVisible(true);
            CursorManager.Instance?.HideCursor();
            base.OnDestroy();
        }

        // ---- 列表 ----

        private void RebuildList()
        {
            if (_questListRoot == null || _questItemTemplate == null) return;

            for (int i = _questListRoot.childCount - 1; i >= 0; i--)
            {
                var child = _questListRoot.GetChild(i);
                if (child == _questItemTemplate) continue;
                Object.Destroy(child.gameObject);
            }

            int firstShown = 0;
            var all = QuestConfigMgr.Instance.GetAll();
            for (int i = 0; i < all.Count; i++)
            {
                var cfg = all[i];
                var state = QuestSystem.GetState(cfg.Id);
                // 未接取且不满足接取条件的任务不显示（避免剧透后续任务链）
                if (state == QuestState.Inactive && !QuestSystem.CanAccept(cfg.Id)) continue;

                var item = Object.Instantiate(_questItemTemplate.gameObject, _questListRoot);
                item.name = $"m_btn_Quest_{cfg.Id}";
                item.SetActive(true);

                var text = item.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = $"[{GetStateLabel(state)}] {cfg.Name}";
                }

                var btn = item.GetComponent<Button>();
                if (btn != null)
                {
                    int questId = cfg.Id;
                    btn.onClick.AddListener(() => ShowDetail(questId));
                }

                if (firstShown == 0)
                {
                    firstShown = cfg.Id;
                }
            }

            ShowDetail(firstShown);
        }

        private static string GetStateLabel(QuestState state)
        {
            switch (state)
            {
                case QuestState.Active: return "Active";
                case QuestState.ReadyToTurnIn: return "Ready";
                case QuestState.Completed: return "Done";
                default: return "Available";
            }
        }

        // ---- 详情 ----

        private void ShowDetail(int questId)
        {
            _selectedQuestId = questId;
            var cfg = questId != 0 ? QuestConfigMgr.Instance.Get(questId) : null;
            if (cfg == null)
            {
                if (_nameText != null) _nameText.text = "No quest selected";
                if (_descText != null) _descText.text = string.Empty;
                if (_objectivesText != null) _objectivesText.text = string.Empty;
                if (_rewardsText != null) _rewardsText.text = string.Empty;
                return;
            }

            if (_nameText != null)
            {
                _nameText.text = cfg.Name;
            }
            if (_descText != null)
            {
                _descText.text = cfg.Desc;
            }

            if (_objectivesText != null)
            {
                var sb = new StringBuilder();
                foreach (var obj in QuestConfigMgr.Instance.GetObjectives(questId))
                {
                    int cur = Mathf.Min(QuestSystem.GetProgress(obj.Id), obj.Count);
                    sb.AppendLine($"{obj.Desc}  {cur}/{obj.Count}");
                }
                _objectivesText.text = sb.ToString();
            }

            if (_rewardsText != null)
            {
                _rewardsText.text = $"Rewards: {FormatRewards(cfg.Rewards)}";
            }
        }

        /// <summary>
        /// 奖励串转显示文本：gold:200|exp:50|item:10005:2 → "200G + 50EXP + Stone x2"。
        /// </summary>
        private static string FormatRewards(string rewards)
        {
            if (string.IsNullOrWhiteSpace(rewards)) return "-";
            var parts = new List<string>();
            foreach (var term in rewards.Split('|'))
            {
                var seg = term.Split(':');
                switch (seg[0].Trim())
                {
                    case "gold" when seg.Length == 2:
                        parts.Add($"{seg[1]}G");
                        break;
                    case "exp" when seg.Length == 2:
                        parts.Add($"{seg[1]}EXP");
                        break;
                    case "item" when seg.Length == 3 && int.TryParse(seg[1], out int itemId):
                        var item = ConfigSystem.Instance.Tables.TbItem.GetOrDefault(itemId);
                        parts.Add($"{(item != null ? item.Name : $"Item#{itemId}")} x{seg[2]}");
                        break;
                }
            }
            return parts.Count > 0 ? string.Join(" + ", parts) : rewards;
        }
    }
}
