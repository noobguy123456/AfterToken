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
        private Slider _scopeSensitivitySlider;
        private TextMeshProUGUI _scopeSensitivityValueText;
        private Toggle _sniperAimModeToggle;
        private Button _closeButton;

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            // FindChildComponent 基于 transform.Find（不递归），控件均位于 m_rect_ContentRoot 下，必须写完整路径。
            _sensitivitySlider = FindChildComponent<Slider>("m_rect_ContentRoot/m_slider_Sensitivity");
            _sensitivityValueText = FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_text_SensitivityValue");
            _scopeSensitivitySlider = FindChildComponent<Slider>("m_rect_ContentRoot/m_slider_ScopeSensitivity");
            _scopeSensitivityValueText = FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_text_ScopeSensitivityValue");
            _sniperAimModeToggle = FindChildComponent<Toggle>("m_rect_ContentRoot/m_toggle_SniperAimMode");
            _closeButton = FindChildComponent<Button>("m_rect_ContentRoot/m_btn_Close");
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            CursorManager.Instance?.ShowCursor();
            InitializeSensitivity();
            if (_sniperAimModeToggle != null)
            {
                _sniperAimModeToggle.isOn = SniperAimModeSetting.IsToggle;
            }
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            if (_sensitivitySlider != null)
            {
                _sensitivitySlider.onValueChanged.RemoveAllListeners();
                _sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            }
            if (_scopeSensitivitySlider != null)
            {
                _scopeSensitivitySlider.onValueChanged.RemoveAllListeners();
                _scopeSensitivitySlider.onValueChanged.AddListener(OnScopeSensitivityChanged);
            }
            if (_sniperAimModeToggle != null)
            {
                _sniperAimModeToggle.onValueChanged.RemoveAllListeners();
                _sniperAimModeToggle.onValueChanged.AddListener(isOn => SniperAimModeSetting.IsToggle = isOn);
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
            SniperAimModeSetting.Save();
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
            if (_scopeSensitivitySlider != null)
            {
                _scopeSensitivitySlider.minValue = SensitivitySetting.Min;
                _scopeSensitivitySlider.maxValue = SensitivitySetting.Max;
                _scopeSensitivitySlider.value = SensitivitySetting.ScopedValue;
                _scopeSensitivitySlider.wholeNumbers = false;
            }

            UpdateSensitivityText(SensitivitySetting.Value);
            UpdateScopeSensitivityText(SensitivitySetting.ScopedValue);
        }

        private void OnSensitivityChanged(float value)
        {
            SensitivitySetting.Value = value;
            UpdateSensitivityText(value);
        }

        private void OnScopeSensitivityChanged(float value)
        {
            SensitivitySetting.ScopedValue = value;
            UpdateScopeSensitivityText(value);
        }

        private void UpdateSensitivityText(float value)
        {
            if (_sensitivityValueText != null)
            {
                _sensitivityValueText.text = $"Sensitivity: {value:F2}";
            }
        }

        private void UpdateScopeSensitivityText(float value)
        {
            if (_scopeSensitivityValueText != null)
            {
                _scopeSensitivityValueText.text = $"Scope Sensitivity: {value:F2}";
            }
        }
    }
}
