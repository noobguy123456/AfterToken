using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic.Loot;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 战利品容器系统。
    /// 管理场景中的容器实体：交互提示、E 键开箱、箱内道具拿取进临时背包。
    /// 注意：与 PortalSystem 共用 OnInteractPressed，两者触发区不要重叠摆放
    /// （统一 IInteractable 仲裁器待做，见 docs/TODO.md）。
    /// </summary>
    public class LootContainerSystem : MonoBehaviour
    {
        public static LootContainerSystem Instance { get; private set; }

        private readonly GameEventMgr _eventMgr = new GameEventMgr();

        private LootContainerEntity _currentContainer;
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
        /// 玩家进入容器触发区。
        /// </summary>
        public void OnPlayerEnteredContainer(LootContainerEntity container)
        {
            _currentContainer = container;
            if (!container.IsOpened)
            {
                ShowPrompt("Press E to Open");
            }
        }

        /// <summary>
        /// 玩家离开容器触发区：关提示，同时关掉开着的箱子面板。
        /// </summary>
        public void OnPlayerExitedContainer(LootContainerEntity container)
        {
            if (_currentContainer == container)
            {
                _currentContainer = null;
                HidePrompt();
            }

            if (GameModule.UI.HasWindow<LootContainerUI>())
            {
                GameModule.UI.CloseUI<LootContainerUI>();
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
            if (GameModule.UI.HasWindow<LootContainerUI>())
            {
                GameModule.UI.CloseUI<LootContainerUI>();
                return;
            }

            if (_currentContainer == null || _currentContainer.IsOpened)
            {
                return;
            }

            _currentContainer.EnsureContentsRolled();
            GameModule.UI.ShowUIAsync<LootContainerUI>(_currentContainer);
            HidePrompt();
        }

        /// <summary>
        /// 拿取指定格的道具进临时背包。成功返回 true（背包满时 RunInventory 已发 OnInventoryFull）。
        /// </summary>
        public bool TryTake(LootContainerEntity container, int index)
        {
            if (container == null || index < 0 || index >= container.Contents.Count)
            {
                return false;
            }

            var stack = container.Contents[index];
            if (!RunInventory.TryAdd(stack.ItemId, stack.Count))
            {
                return false;
            }

            container.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// 一键全拿。背包放不下时跳过该格继续拿后面的，返回是否有剩余。
        /// </summary>
        public bool TakeAll(LootContainerEntity container)
        {
            if (container == null) return false;

            for (int i = container.Contents.Count - 1; i >= 0; i--)
            {
                TryTake(container, i);
            }
            return container.Contents.Count > 0;
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
                // LootContainerSystem 销毁时取消，忽略异常。
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
