using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic.Narrative;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 小纸条系统。
    /// 管理场景中的纸条实体：交互提示、E 键阅读（打开 NoteUI）。
    /// 注意：与 PortalSystem/LootContainerSystem 共用 OnInteractPressed，触发区不要重叠摆放
    /// （统一 IInteractable 仲裁器待做，见 docs/TODO.md）。
    /// </summary>
    public class NoteSystem : MonoBehaviour
    {
        public static NoteSystem Instance { get; private set; }

        private readonly GameEventMgr _eventMgr = new GameEventMgr();

        private NoteEntity _currentNote;
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
        /// 玩家进入纸条触发区。
        /// </summary>
        public void OnPlayerEnteredNote(NoteEntity note)
        {
            _currentNote = note;
            ShowPrompt("Press E to Read");
        }

        /// <summary>
        /// 玩家离开纸条触发区：关提示，同时关掉开着的纸条面板。
        /// </summary>
        public void OnPlayerExitedNote(NoteEntity note)
        {
            if (_currentNote == note)
            {
                _currentNote = null;
                HidePrompt();
            }

            if (GameModule.UI.HasWindow<NoteUI>())
            {
                GameModule.UI.CloseUI<NoteUI>();
            }
        }

        private void OnInteractPressed()
        {
            // 死亡判定闸：与 PortalSystem 一致，死亡后禁止交互
            var player = PlayerSystem.Instance?.GetPlayerEntity();
            if (player == null || player.IsDead)
            {
                return;
            }

            // 面板开着时再按 E = 关闭
            if (GameModule.UI.HasWindow<NoteUI>())
            {
                GameModule.UI.CloseUI<NoteUI>();
                return;
            }

            if (_currentNote == null)
            {
                return;
            }

            var cfg = NoteConfigMgr.Instance.Get(_currentNote.NoteId);
            if (cfg == null)
            {
                Log.Warning($"[NoteSystem] 找不到纸条配置 id={_currentNote.NoteId}");
                return;
            }

            GameModule.UI.ShowUIAsync<NoteUI>(cfg);
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
                // NoteSystem 销毁时取消，忽略异常。
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
