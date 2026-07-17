using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 全屏转场遮罩 UI。
    /// </summary>
    [Window(UILayer.System, location: "TransitionUI", fullScreen: true)]
    public class TransitionUI : UIWindow
    {
        private CanvasGroup _canvasGroup;
        private Image _overlayImage;

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            _overlayImage = FindChildComponent<Image>("m_img_Overlay");
            _canvasGroup = FindChildComponent<CanvasGroup>("m_img_Overlay");
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
        }

        /// <summary>
        /// 播放渐变到不透明。
        /// </summary>
        public async UniTask FadeInAsync(float duration)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.blocksRaycasts = true;
            await TweenAlpha(_canvasGroup, 1f, duration);
        }

        /// <summary>
        /// 播放渐变到透明。
        /// </summary>
        public async UniTask FadeOutAsync(float duration)
        {
            if (_canvasGroup == null) return;
            await TweenAlpha(_canvasGroup, 0f, duration);
            _canvasGroup.blocksRaycasts = false;
        }

        private async UniTask TweenAlpha(CanvasGroup group, float targetAlpha, float duration)
        {
            if (duration <= 0f)
            {
                group.alpha = targetAlpha;
                return;
            }

            float startAlpha = group.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            }
            group.alpha = targetAlpha;
        }
    }
}
