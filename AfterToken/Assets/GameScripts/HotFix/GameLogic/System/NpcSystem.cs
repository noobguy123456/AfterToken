using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// NPC 系统。
    /// 管理场景中的 NPC 实体：交互提示（"Press E to Talk"）、E 键交谈。
    /// NPC 配了 dialogueId 时驱动 <see cref="DialogueSystem"/> 打开对话窗口
    /// （设计见 docs/Proposal/narrative/dialogue-system.md）。
    /// 挂载：ProcedureSimulation 的 SimulationRoot（NPC 驻基地；战斗场景不对话）。
    /// 注意：与 PortalSystem 共用 OnInteractPressed，触发区不要重叠摆放
    /// （统一 IInteractable 仲裁器待做，见 docs/TODO.md）。
    /// </summary>
    public class NpcSystem : MonoBehaviour
    {
        public static NpcSystem Instance { get; private set; }

        /// <summary>是否有 NPC 交互提示激活中（队友信息提示据此避让，避免共用 InteractionPromptUI 时互相覆盖）。</summary>
        public bool HasActivePrompt => _currentNpc != null;

        private readonly GameEventMgr _eventMgr = new GameEventMgr();

        private NpcEntity _currentNpc;
        private InteractionPromptUI _promptUI;
        private CancellationTokenSource _promptCts;

        private void Awake()
        {
            Instance = this;
            _eventMgr.AddEvent(IBattleInputEvent_Event.OnInteractPressed, OnInteractPressed);
            _eventMgr.AddEvent<int>(IDialogueEvent_Event.OnDialogueEnded, OnDialogueEnded);
        }

        private void OnDestroy()
        {
            _eventMgr.Clear();
            Instance = null;

            _promptCts?.Cancel();
            _promptCts?.Dispose();
            _promptCts = null;
        }

        /// <summary>
        /// 玩家进入 NPC 触发区。
        /// </summary>
        public void OnPlayerEnteredNpc(NpcEntity npc)
        {
            _currentNpc = npc;
            ShowPrompt(GetInteractPrompt(npc.NpcId));
        }

        /// <summary>
        /// 玩家离开 NPC 触发区：关提示，并打断进行中的对话。
        /// </summary>
        public void OnPlayerExitedNpc(NpcEntity npc)
        {
            DialogueSystem.Instance?.OnNpcOutOfRange(npc.NpcId);
            if (_currentNpc == npc)
            {
                _currentNpc = null;
                HidePrompt();
            }
        }

        private void OnInteractPressed()
        {
            if (_currentNpc == null)
            {
                return;
            }

            // 对话进行中 E 是推进键，不重复触发交谈（DialogueSystem.Update 消费）
            if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsPlaying)
            {
                return;
            }

            var cfg = NpcConfigMgr.Instance.Get(_currentNpc.NpcId);
            if (cfg == null)
            {
                Log.Warning($"[NpcSystem] 找不到 NPC 配置 id={_currentNpc.NpcId}");
                return;
            }

            Log.Info($"[NpcSystem] 与 NPC 交互: {cfg.Name} (id={cfg.Id})");

            // 操作台类 NPC（consoleType=1 技能树）：直接开功能界面，不走对话
            if (cfg.ConsoleType == 1)
            {
                HidePrompt();
                GameEvent.Get<INpcEvent>()?.OnNpcTalked(cfg.Id);
                GameModule.UI.ShowUIAsync<SkillTreeUI>();
                return;
            }

            GameEvent.Get<INpcEvent>()?.OnNpcTalked(cfg.Id);

            // 配了对话则驱动对话窗口；对话期间收起交互提示
            if (cfg.DialogueId > 0 && DialogueSystem.Instance != null)
            {
                HidePrompt();
                DialogueSystem.Instance.StartDialogue(cfg.DialogueId, cfg.Id);
            }
            else
            {
                Log.Info($"[NpcSystem] NPC id={cfg.Id} 未配置对话（dialogueId=0），暂无对话内容");
            }
        }

        /// <summary>
        /// 对话结束：玩家还在触发区内则恢复交互提示。
        /// </summary>
        private void OnDialogueEnded(int dialogueId)
        {
            if (_currentNpc != null)
            {
                ShowPrompt(GetInteractPrompt(_currentNpc.NpcId));
            }
        }

        /// <summary>
        /// 按 NPC 类型取交互提示文案：操作台（consoleType=1）用专用词条，其余用交谈词条。
        /// </summary>
        private static string GetInteractPrompt(int npcId)
        {
            string keyName = KeyBindingSetting.GetKeyDisplayName(KeyBindingSetting.GetKey(KeyBindAction.Interact));
            var cfg = NpcConfigMgr.Instance.Get(npcId);
            if (cfg != null && cfg.ConsoleType == 1)
            {
                return Loc.Get("ui.interact.skill_console", keyName);
            }
            return Loc.Get("ui.interact.talk", keyName);
        }

        private void ShowPrompt(string text)
        {
            ShowPromptAsync(text).Forget();
        }

        private async UniTaskVoid ShowPromptAsync(string text)
        {
            _promptCts?.Cancel();
            _promptCts?.Dispose();
            _promptCts = new CancellationTokenSource();

            try
            {
                if (_promptUI == null)
                {
                    _promptUI = await GameModule.UI.ShowUIAsyncAwait<InteractionPromptUI>(_promptCts.Token);
                }
                _promptUI?.SetPrompt(text);
            }
            catch (OperationCanceledException)
            {
                // NpcSystem 销毁时取消，忽略异常。
            }
        }

        private void HidePrompt()
        {
            if (_promptUI != null)
            {
                GameModule.UI.CloseUI<InteractionPromptUI>();
                _promptUI = null;
            }
        }
    }
}
