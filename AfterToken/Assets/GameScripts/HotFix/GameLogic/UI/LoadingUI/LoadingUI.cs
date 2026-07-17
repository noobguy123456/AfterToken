using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 加载过渡 UI。
    /// </summary>
    [Window(UILayer.System, location: "LoadingUI", fullScreen: true)]
    public class LoadingUI : UIWindow
    {
        private Slider _progressSlider;
        private TextMeshProUGUI _progressText;

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            _progressSlider = FindChildComponent<Slider>("m_slider_Progress");
            _progressText = FindChildComponent<TextMeshProUGUI>("m_text_Progress");
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            SetProgress(0f);
        }

        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (_progressSlider != null)
            {
                _progressSlider.value = progress;
            }
            if (_progressText != null)
            {
                _progressText.text = $"Loading... {progress * 100:F0}%";
            }
        }
    }
}
