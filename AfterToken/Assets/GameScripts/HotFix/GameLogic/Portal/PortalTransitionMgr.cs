using System;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic.Portal
{
    /// <summary>
    /// 传送门转场管理器。
    /// </summary>
    public static class PortalTransitionMgr
    {
        /// <summary>
        /// 播放转场并执行场景切换。
        /// 转场到最暗后先检查中止条件（如玩家在转场期间死亡）：满足则渐出并放弃切换，
        /// 否则调用回调完成流程切换（由新流程负责关闭转场 UI）。
        /// </summary>
        /// <param name="transitionType">转场类型。</param>
        /// <param name="duration">转场时长。</param>
        /// <param name="shouldAbort">转场完成前的中止判定，返回 true 表示放弃本次传送。</param>
        /// <param name="onAborted">传送被中止时的回调（用于清理已保存的状态等）。</param>
        /// <param name="onTransitionComplete">确认执行切换时调用的回调。</param>
        public static async UniTask PlayAsync(string transitionType, float duration, Func<bool> shouldAbort, Action onAborted, Action onTransitionComplete)
        {
            var transitionUI = await GameModule.UI.ShowUIAsyncAwait<TransitionUI>();
            if (transitionUI == null)
            {
                Log.Warning("[PortalTransitionMgr] Failed to show TransitionUI.");
                onTransitionComplete?.Invoke();
                return;
            }

            await transitionUI.FadeInAsync(duration);

            if (shouldAbort != null && shouldAbort())
            {
                await transitionUI.FadeOutAsync(duration);
                GameModule.UI.CloseUI<TransitionUI>();
                onAborted?.Invoke();
                return;
            }

            onTransitionComplete?.Invoke();
        }
    }
}
