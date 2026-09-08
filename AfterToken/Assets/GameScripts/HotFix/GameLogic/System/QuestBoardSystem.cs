using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic.Narrative;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 任务板系统。
    /// 管理基地中的任务板实体：交互提示（词条 ui.questboard.prompt）、E 键打开 QuestBoardUI。
    /// 挂载：ProcedureSimulation 的 SimulationRoot。
    /// 注意：与 PortalSystem/NpcSystem/NoteSystem 共用 OnInteractPressed，触发区不要重叠摆放
    /// （统一 IInteractable 仲裁器待做，见 docs/TODO.md）。
    /// </summary>
    public class QuestBoardSystem : MonoBehaviour
    {
        public static QuestBoardSystem Instance { get; private set; }

        private readonly GameEventMgr _eventMgr = new GameEventMgr();

        private QuestBoardEntity _currentBoard;
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
        /// 玩家进入任务板触发区。
        /// </summary>
        public void OnPlayerEnteredBoard(QuestBoardEntity board)
        {
            _currentBoard = board;
            ShowPrompt(Loc.Get("ui.questboard.prompt"));
        }

        /// <summary>
        /// 玩家离开任务板触发区：关提示，同时关掉开着的任务板面板。
        /// </summary>
        public void OnPlayerExitedBoard(QuestBoardEntity board)
        {
            if (_currentBoard == board)
            {
                _currentBoard = null;
                HidePrompt();
            }

            if (GameModule.UI.HasWindow<QuestBoardUI>())
            {
                GameModule.UI.CloseUI<QuestBoardUI>();
            }
        }

        private void OnInteractPressed()
        {
            // 死亡判定闸：死亡后禁止交互。基地场景 PlayerEntity 组件被移除（ProcedureSimulation），
            // 拿不到实体时不拦截（否则基地内永远无法交互，与 NoteSystem 的战斗场景语义不同）
            var player = PlayerSystem.Instance?.GetPlayerEntity();
            if (player != null && player.IsDead)
            {
                return;
            }

            // 面板开着时再按 E = 关闭
            if (GameModule.UI.HasWindow<QuestBoardUI>())
            {
                GameModule.UI.CloseUI<QuestBoardUI>();
                return;
            }

            if (_currentBoard == null)
            {
                return;
            }

            GameModule.UI.ShowUIAsync<QuestBoardUI>();
            HidePrompt();
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
                // QuestBoardSystem 销毁时取消，忽略异常。
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
