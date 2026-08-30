using System.Text;
using GameConfig.cfg;
using TMPro;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 任务追踪 HUD：屏幕左侧常驻列出击破中任务（Active/ReadyToTurnIn）的名称与目标进度。
    /// 纯展示，不交互、不挡射线（prefab 上文本 raycastTarget 全关、根 CanvasGroup blocksRaycasts=false）。
    /// 不暂停游戏；不管光标/准星。基地（ProcedureSimulation）与战斗（ProcedureBattle）均常驻打开。
    /// 数据来自 QuestSystem，订阅 <see cref="IQuestEvent"/> 四事件实时刷新。
    /// </summary>
    [Window(UILayer.UI, location: "QuestTrackerUI", fullScreen: false)]
    public class QuestTrackerUI : UIWindow
    {
        /// <summary>HUD 不暂停游戏。</summary>
        public override float TimeScaleWhenVisible => InspectorTimeScale ?? 1f;

        private static readonly UnityEngine.Color ActiveNameColor = new UnityEngine.Color(1f, 0.85f, 0.2f);
        private static readonly UnityEngine.Color ReadyNameColor = new UnityEngine.Color(0.35f, 1f, 0.35f);

        private RectTransform _listRoot;
        private RectTransform _itemTemplate;

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            _listRoot = FindChildComponent<RectTransform>("m_rect_Tracker");
            _itemTemplate = FindChildComponent<RectTransform>("m_rect_Tracker/m_rect_QuestTemplate");
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            Rebuild();
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent<int>(IQuestEvent_Event.OnQuestAccepted, _ => Rebuild());
            AddUIEvent<int>(IQuestEvent_Event.OnQuestReadyToTurnIn, _ => Rebuild());
            AddUIEvent<int>(IQuestEvent_Event.OnQuestCompleted, _ => Rebuild());
            AddUIEvent<int, int, int, int>(IQuestEvent_Event.OnObjectiveProgress, (_, _, _, _) => Rebuild());
        }

        /// <summary>
        /// 重建追踪列表（任务数少，直接全量重建）。
        /// </summary>
        private void Rebuild()
        {
            if (_listRoot == null || _itemTemplate == null) return;

            for (int i = _listRoot.childCount - 1; i >= 0; i--)
            {
                var child = _listRoot.GetChild(i);
                if (child == _itemTemplate) continue;
                Object.Destroy(child.gameObject);
            }

            var active = QuestSystem.GetActiveQuests();
            foreach (var questId in active)
            {
                var cfg = QuestConfigMgr.Instance.Get(questId);
                if (cfg == null) continue;

                var item = Object.Instantiate(_itemTemplate.gameObject, _listRoot);
                item.name = $"m_rect_Quest_{questId}";
                item.SetActive(true);

                var texts = item.GetComponentsInChildren<TextMeshProUGUI>(true);
                TextMeshProUGUI nameText = null;
                TextMeshProUGUI objectivesText = null;
                foreach (var t in texts)
                {
                    if (t.name == "m_text_Name") nameText = t;
                    else if (t.name == "m_text_Objectives") objectivesText = t;
                }

                bool ready = QuestSystem.GetState(questId) == QuestState.ReadyToTurnIn;
                if (nameText != null)
                {
                    nameText.text = ready ? $"{cfg.Name} (Ready)" : cfg.Name;
                    nameText.color = ready ? ReadyNameColor : ActiveNameColor;
                }
                if (objectivesText != null)
                {
                    var sb = new StringBuilder();
                    foreach (var obj in QuestConfigMgr.Instance.GetObjectives(questId))
                    {
                        int cur = Mathf.Min(QuestSystem.GetProgress(obj.Id), obj.Count);
                        sb.AppendLine($"{obj.Desc}  {cur}/{obj.Count}");
                    }
                    objectivesText.text = sb.ToString().TrimEnd();
                }
            }
        }
    }
}
