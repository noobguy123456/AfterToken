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
        /// 转场到最暗后调用回调，随后由流程切换关闭当前场景与 UI，不再执行渐出。
        /// </summary>
        /// <param name="transitionType">转场类型。</param>
        /// <param name="duration">转场时长。</param>
        /// <param name="onTransitionComplete">转场到最暗后执行的回调。</param>
        public static async UniTask PlayAsync(string transitionType, float duration, Action onTransitionComplete)
        {
            var transitionUI = await GameModule.UI.ShowUIAsyncAwait<TransitionUI>();
            if (transitionUI == null)
            {
                Log.Warning("[PortalTransitionMgr] Failed to show TransitionUI.");
                onTransitionComplete?.Invoke();
                return;
            }

            await transitionUI.FadeInAsync(duration);
            onTransitionComplete?.Invoke();
        }
    }
}
