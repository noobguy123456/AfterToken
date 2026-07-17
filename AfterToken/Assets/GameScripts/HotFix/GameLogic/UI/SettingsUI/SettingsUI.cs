using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 设置面板。
    /// 提供准星灵敏度等游戏内可调选项。
    /// </summary>
    [Window(UILayer.Top, location: "SettingsUI", fullScreen: false)]
    public class SettingsUI : UIWindow
    {
        /// <summary>
        /// 设置面板打开时暂停游戏进程（不影响声音）。
        /// 若 UI Prefab 上挂了 UIWindowTimeScale，Inspector 值可覆盖此处默认值。
        /// </summary>
        public override float TimeScaleWhenVisible => InspectorTimeScale ?? 0f;

        private Slider _sensitivitySlider;
        private TextMeshProUGUI _sensitivityValueText;
        private Button _closeButton;

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            _sensitivitySlider = FindChildComponent<Slider>("m_slider_Sensitivity");
            _sensitivityValueText = FindChildComponent<TextMeshProUGUI>("m_text_SensitivityValue");
            _closeButton = FindChildComponent<Button>("m_btn_Close");
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            CursorManager.Instance?.ShowCursor();
            InitializeSensitivity();
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            if (_sensitivitySlider != null)
            {
                _sensitivitySlider.onValueChanged.RemoveAllListeners();
                _sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            }
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(() => GameModule.UI.CloseUI<SettingsUI>());
            }
        }

        protected override void OnDestroy()
        {
            CursorManager.Instance?.HideCursor();
            SensitivitySetting.Save();
            base.OnDestroy();
        }

        private void InitializeSensitivity()
        {
            if (_sensitivitySlider != null)
            {
                _sensitivitySlider.minValue = SensitivitySetting.Min;
                _sensitivitySlider.maxValue = SensitivitySetting.Max;
                _sensitivitySlider.value = SensitivitySetting.Value;
                _sensitivitySlider.wholeNumbers = false;
            }

            UpdateSensitivityText(SensitivitySetting.Value);
        }

        private void OnSensitivityChanged(float value)
        {
            SensitivitySetting.Value = value;
            UpdateSensitivityText(value);
        }

        private void UpdateSensitivityText(float value)
        {
            if (_sensitivityValueText != null)
            {
                _sensitivityValueText.text = $"Sensitivity: {value:F2}";
            }
        }
    }
}
