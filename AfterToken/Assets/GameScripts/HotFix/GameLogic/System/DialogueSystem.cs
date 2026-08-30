using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameConfig.cfg;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 对话系统（解释器）。
    /// 数据（TbDialogue/TbDialogueNode 扁平节点表）→ 本系统解释执行 → IDialogueEvent → DialogueUI 呈现。
    /// 状态机：Idle → Playing(line) → WaitingChoice → Idle。
    /// 操作：E/回车 推进（打字中先补全），数字键 1~4 选选项，Esc 由 SimulationInputSystem 关窗链调用 <see cref="EndDialogue"/>。
    /// 挂载：ProcedureSimulation 的 SimulationRoot（基地对话；战斗场景不对话）。
    /// 任务枢纽：NPC 有可交付/可接取任务时，对话不从任务节点链自动开始，
    /// 而是先播默认问候语并出选项（Turn in/Accept 任务项 + Just chatting），常见 RPG 的 NPC 选项式交互。
    /// 设计文档：docs/Proposal/narrative/dialogue-system.md。
    /// </summary>
    public class DialogueSystem : MonoBehaviour
    {
        public static DialogueSystem Instance { get; private set; }

        /// <summary>对话是否进行中（其它系统用它屏蔽 E 交互等输入）。</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>当前交谈的 NPC id（0 = 无）。</summary>
        public int CurrentNpcId { get; private set; }

        private Dialogue _dialogue;
        private DialogueNode _currentNode;
        private List<KeyValuePair<string, int>> _choices;
        private bool _waitingChoice;
        private int _startFrame = -1;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
            _dialogue = null;
            _currentNode = null;
            _choices = null;
        }

        /// <summary>
        /// 开始一段对话（NpcSystem 在 E 交谈时调用）。
        /// </summary>
        public void StartDialogue(int dialogueId, int npcId)
        {
            StartDialogueAsync(dialogueId, npcId).Forget();
        }

        private async UniTaskVoid StartDialogueAsync(int dialogueId, int npcId)
        {
            if (IsPlaying) return;

            var cfg = DialogueConfigMgr.Instance.Get(dialogueId);
            if (cfg == null)
            {
                Log.Warning($"[DialogueSystem] 找不到对话配置 id={dialogueId}");
                return;
            }
            if (cfg.OnceOnly && DialogueFlagSystem.Has(DialogueFlagSystem.SeenPrefix + dialogueId))
            {
                return; // 只触发一次的对话已读过
            }

            var start = DialogueConfigMgr.Instance.GetStartNode(dialogueId);
            if (start == null)
            {
                Log.Warning($"[DialogueSystem] 对话 id={dialogueId} 没有满足条件的起始节点");
                return;
            }

            // 先置状态再异步开窗：同帧的 E 键不会再触发交互（_startFrame 防连跳）
            _dialogue = cfg;
            CurrentNpcId = npcId;
            IsPlaying = true;
            _startFrame = Time.frameCount;

            // 先开窗再发事件，保证 UI 的事件订阅已就绪
            await GameModule.UI.ShowUIAsyncAwait<DialogueUI>();
            if (!IsPlaying) return; // 开窗期间被外部打断

            GameEvent.Get<IDialogueEvent>()?.OnDialogueStarted(dialogueId, npcId);

            // 任务枢纽：有可交付/可接取任务时出选项菜单，否则直接走节点链
            var questChoices = CollectQuestChoices(dialogueId);
            if (questChoices.Count > 0)
            {
                ShowQuestHub(dialogueId, questChoices);
            }
            else
            {
                PlayNode(start);
            }
        }

        /// <summary>
        /// 收集该对话的任务选项：扫描节点表，把 condition 为 quest:id:ready / quest:id:accept 的节点
        /// 按当前任务状态映射为"Turn in:/Accept:"选项（可交付在前）。无任务交互时返回空表。
        /// </summary>
        private static List<KeyValuePair<string, int>> CollectQuestChoices(int dialogueId)
        {
            var readyChoices = new List<KeyValuePair<string, int>>();
            var acceptChoices = new List<KeyValuePair<string, int>>();

            foreach (var node in ConfigSystem.Instance.Tables.TbDialogueNode.DataList)
            {
                if (node.DialogueId != dialogueId || string.IsNullOrEmpty(node.Condition)) continue;

                var parts = node.Condition.Split(':');
                if (parts.Length != 3 || parts[0] != "quest") continue;
                if (!int.TryParse(parts[1], out int questId)) continue;

                var questCfg = QuestConfigMgr.Instance.Get(questId);
                if (questCfg == null) continue;

                switch (parts[2])
                {
                    case "ready" when QuestSystem.GetState(questId) == QuestState.ReadyToTurnIn:
                        readyChoices.Add(new KeyValuePair<string, int>($"Turn in: {questCfg.Name}", node.Id));
                        break;
                    case "accept" when QuestSystem.CanAccept(questId):
                        acceptChoices.Add(new KeyValuePair<string, int>($"Accept: {questCfg.Name}", node.Id));
                        break;
                }
            }

            readyChoices.AddRange(acceptChoices);
            return readyChoices;
        }

        /// <summary>
        /// 任务枢纽：播默认问候语（无条件起始节点），选项为任务项 + Just chatting（走默认问候的后续节点）。
        /// </summary>
        private void ShowQuestHub(int dialogueId, List<KeyValuePair<string, int>> questChoices)
        {
            var defaultNode = FindDefaultNode(dialogueId);

            // 选项上限 4（DialogueUI.MaxChoices）：任务项最多 3 条 + 正常对话
            _choices = new List<KeyValuePair<string, int>>(questChoices);
            if (_choices.Count > 3)
            {
                _choices.RemoveRange(3, _choices.Count - 3);
            }
            _choices.Add(new KeyValuePair<string, int>("Just chatting.", defaultNode != null ? defaultNode.Next : 0));

            _currentNode = defaultNode;
            _waitingChoice = true;
            if (defaultNode != null)
            {
                GameEvent.Get<IDialogueEvent>()?.OnDialogueLine(defaultNode.Id, defaultNode.Speaker, defaultNode.Text);
            }
            var options = new string[_choices.Count];
            for (int i = 0; i < _choices.Count; i++)
            {
                options[i] = _choices[i].Key;
            }
            GameEvent.Get<IDialogueEvent>()?.OnDialogueChoices(0, options);
        }

        /// <summary>
        /// 默认（无条件）起始节点：从 startNode 起第一个无条件的节点，作为任务枢纽的问候语与"正常对话"入口。
        /// </summary>
        private static DialogueNode FindDefaultNode(int dialogueId)
        {
            var cfg = DialogueConfigMgr.Instance.Get(dialogueId);
            if (cfg == null) return null;

            var node = DialogueConfigMgr.Instance.GetNode(cfg.StartNode);
            int guard = 0;
            while (node != null && guard++ < 1000)
            {
                if (string.IsNullOrEmpty(node.Condition)) return node;
                node = node.Next != 0 ? DialogueConfigMgr.Instance.GetNode(node.Next) : null;
            }
            return null;
        }

        /// <summary>
        /// 到达并播放节点：条件不满足的节点跳过；setflag/end 不占 UI 拍子，连续执行。
        /// </summary>
        private void PlayNode(DialogueNode node)
        {
            int guard = 0;
            while (node != null && !NarrativeCondition.Evaluate(node.Condition) && guard++ < 1000)
            {
                node = node.Next != 0 ? DialogueConfigMgr.Instance.GetNode(node.Next) : null;
            }

            if (node == null)
            {
                EndDialogue();
                return;
            }

            // 到达本节点即执行动作（line/choice/setflag 都可携带）
            NarrativeAction.Execute(node.Action);

            switch (node.Type)
            {
                case "line":
                    _currentNode = node;
                    _waitingChoice = false;
                    GameEvent.Get<IDialogueEvent>()?.OnDialogueLine(node.Id, node.Speaker, node.Text);
                    break;

                case "choice":
                    var choices = ParseChoices(node.Choices);
                    if (choices.Count == 0)
                    {
                        // 没配选项的 choice 视为普通 line
                        PlayNode(node.Next != 0 ? DialogueConfigMgr.Instance.GetNode(node.Next) : null);
                        return;
                    }
                    _currentNode = node;
                    _choices = choices;
                    _waitingChoice = true;
                    if (!string.IsNullOrEmpty(node.Text))
                    {
                        GameEvent.Get<IDialogueEvent>()?.OnDialogueLine(node.Id, node.Speaker, node.Text);
                    }
                    var options = new string[choices.Count];
                    for (int i = 0; i < choices.Count; i++)
                    {
                        options[i] = choices[i].Key;
                    }
                    GameEvent.Get<IDialogueEvent>()?.OnDialogueChoices(node.Id, options);
                    break;

                case "setflag":
                    PlayNode(node.Next != 0 ? DialogueConfigMgr.Instance.GetNode(node.Next) : null);
                    break;

                case "end":
                default:
                    EndDialogue();
                    break;
            }
        }

        /// <summary>
        /// 推进对话（E/回车）：打字中先补全当前行，否则进入下一节点。选项状态下不响应。
        /// </summary>
        public void Advance()
        {
            if (!IsPlaying || _waitingChoice || _currentNode == null) return;

            var ui = GameModule.UI.GetUI<DialogueUI>();
            if (ui != null && ui.IsTyping)
            {
                ui.CompleteLine();
                return;
            }

            PlayNode(_currentNode.Next != 0 ? DialogueConfigMgr.Instance.GetNode(_currentNode.Next) : null);
        }

        /// <summary>
        /// 选择选项（数字键 1~4 或 UI 按钮）。
        /// </summary>
        public void Choose(int index)
        {
            if (!IsPlaying || !_waitingChoice || _choices == null) return;
            if (index < 0 || index >= _choices.Count) return;

            int next = _choices[index].Value;
            PlayNode(next != 0 ? DialogueConfigMgr.Instance.GetNode(next) : null);
        }

        /// <summary>
        /// 结束对话（含 Esc 主动关闭、走出触发区打断）。
        /// </summary>
        public void EndDialogue()
        {
            if (!IsPlaying) return;

            if (_dialogue != null && _dialogue.OnceOnly)
            {
                DialogueFlagSystem.Set(DialogueFlagSystem.SeenPrefix + _dialogue.Id);
            }

            int dialogueId = _dialogue != null ? _dialogue.Id : 0;
            _dialogue = null;
            _currentNode = null;
            _choices = null;
            _waitingChoice = false;
            IsPlaying = false;
            CurrentNpcId = 0;

            GameEvent.Get<IDialogueEvent>()?.OnDialogueEnded(dialogueId);
            GameModule.UI.CloseUI<DialogueUI>();
        }

        /// <summary>
        /// 玩家走出 NPC 触发区（NpcSystem 通知）：正在与该 NPC 交谈则打断对话。
        /// </summary>
        public void OnNpcOutOfRange(int npcId)
        {
            if (IsPlaying && CurrentNpcId == npcId)
            {
                EndDialogue();
            }
        }

        private void Update()
        {
            if (!IsPlaying) return;
            if (Time.frameCount == _startFrame) return; // 防启动对话的同帧 E 连跳
            // 任务接取确认窗开着时，Enter/Esc 归它处理，对话不推进
            if (GameModule.UI.HasWindow<QuestAcceptConfirmUI>()) return;

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
            {
                Advance();
                return;
            }

            if (_waitingChoice)
            {
                for (int i = 0; i < 4 && i < (_choices?.Count ?? 0); i++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    {
                        Choose(i);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 解析选项串："文本1->节点A|文本2->节点B"。
        /// </summary>
        private static List<KeyValuePair<string, int>> ParseChoices(string raw)
        {
            var result = new List<KeyValuePair<string, int>>();
            if (string.IsNullOrWhiteSpace(raw)) return result;

            foreach (var item in raw.Split('|'))
            {
                var pair = item.Split(new[] { "->" }, StringSplitOptions.None);
                if (pair.Length == 2 && int.TryParse(pair[1].Trim(), out int next))
                {
                    result.Add(new KeyValuePair<string, int>(pair[0].Trim(), next));
                }
                else
                {
                    Log.Warning($"[DialogueSystem] 无法解析选项: {item}");
                }
            }
            return result;
        }
    }
}
