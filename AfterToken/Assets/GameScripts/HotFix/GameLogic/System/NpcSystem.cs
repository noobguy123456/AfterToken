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
    /// 交谈目前只发 <see cref="INpcEvent.OnNpcTalked"/> 事件 + 日志占位，
    /// 对话系统落地后由它订阅该事件驱动对话窗口（见 docs/Proposal/narrative/dialogue-system.md）。
    /// 挂载：ProcedureSimulation 的 SimulationRoot（NPC 驻基地；战斗场景不对话）。
    /// 注意：与 PortalSystem 共用 OnInteractPressed，触发区不要重叠摆放
    /// （统一 IInteractable 仲裁器随对话系统 P0 落地，见 docs/TODO.md）。
    /// </summary>
    public class NpcSystem : MonoBehaviour
    {
        public static NpcSystem Instance { get; private set; }

        private readonly GameEventMgr _eventMgr = new GameEventMgr();

        private NpcEntity _currentNpc;
        private InteractionPromptUI _promptUI;
        private CancellationTokenSource _promptCts;

        private void Awake()
        {
            Instance = this;
            _eventMgr.AddEvent(IBattleInputEvent_Event.OnInteractPressed, OnInteractPressed);
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
            ShowPrompt("Press E to Talk");
        }

        /// <summary>
        /// 玩家离开 NPC 触发区：关提示。
        /// </summary>
        public void OnPlayerExitedNpc(NpcEntity npc)
        {
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

            var cfg = NpcConfigMgr.Instance.Get(_currentNpc.NpcId);
            if (cfg == null)
            {
                Log.Warning($"[NpcSystem] 找不到 NPC 配置 id={_currentNpc.NpcId}");
                return;
            }

            // 对话系统 pending：先只发事件 + 日志占位，UI 由对话系统接管
            Log.Info($"[NpcSystem] 与 NPC 交谈: {cfg.Name} (id={cfg.Id})");
            GameEvent.Get<INpcEvent>()?.OnNpcTalked(cfg.Id);
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
