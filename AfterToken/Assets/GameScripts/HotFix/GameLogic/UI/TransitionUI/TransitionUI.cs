using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 全屏转场遮罩 UI。
    /// 默认灰色渐变，后续替换为正式转场资源。
    /// </summary>
    [Window(UILayer.System, location: "TransitionUI", fullScreen: true)]
    public class TransitionUI : UIWindow
    {
        private CanvasGroup _canvasGroup;
        private Image _overlayImage;

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            BuildUI();
        }

        private void BuildUI()
        {
            var rt = rectTransform;
            if (rt == null) return;

            var go = new GameObject("Overlay");
            go.transform.SetParent(rt, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _overlayImage = go.AddComponent<Image>();
            _overlayImage.color = new Color(0.15f, 0.15f, 0.15f, 0f);
            _overlayImage.raycastTarget = true;

            _canvasGroup = go.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
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
