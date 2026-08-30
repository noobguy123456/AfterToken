using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 设置面板。
    /// General 页签：灵敏度/开镜灵敏度/狙击开镜模式/准星样式颜色/返回主界面。
    /// Input 页签：战斗按键改绑（KeyBindingSetting，点击按键按钮后按新键生效，ESC 取消）。
    /// </summary>
    [Window(UILayer.Top, location: "SettingsUI", fullScreen: false)]
    public class SettingsUI : UIWindow
    {
        /// <summary>
        /// 设置面板打开时暂停游戏进程（不影响声音）。
        /// 若 UI Prefab 上挂了 UIWindowTimeScale，Inspector 值可覆盖此处默认值。
        /// </summary>
        public override float TimeScaleWhenVisible => InspectorTimeScale ?? 0f;

        /// <summary>
        /// 是否正在捕获改绑按键。InputSystem 的 ESC 全局处理读取此状态：
        /// 捕获期间 ESC 只用于取消捕获，不触发关 UI。
        /// </summary>
        public static bool IsCapturingKey { get; private set; }

        private Slider _sensitivitySlider;
        private TextMeshProUGUI _sensitivityValueText;
        private Slider _scopeSensitivitySlider;
        private TextMeshProUGUI _scopeSensitivityValueText;
        private Toggle _sniperAimModeToggle;
        private Button _closeButton;
        private Button _returnMainMenuButton;

        // ---- 页签 ----
        private Button _tabGeneralButton;
        private Button _tabInputButton;
        private GameObject _panelGeneral;
        private GameObject _panelInput;

        // ---- General 页签：准星 ----
        private Button _crosshairStyleButton;
        private TextMeshProUGUI _crosshairStyleText;
        private Image _crosshairPreview;
        private Sprite _previewSprite;
        private readonly Button[] _crosshairColorButtons = new Button[CrosshairSetting.PresetColors.Length];

        /// <summary>
        /// 改绑捕获结束的帧号。绑定鼠标键（如 LMB）时，按下完成绑定、松开落在按钮上会再次触发
        /// onClick 重新进入捕获——同帧忽略这次点击。
        /// </summary>
        private int _captureEndFrame = -1;

        // ---- Input 页签（按键改绑） ----
        private TextMeshProUGUI _bindingHintText;
        private Transform _bindingListRoot;
        private RectTransform _bindingRowTemplate;
        private Button _resetBindingsButton;

        private bool _isCapturing;
        private KeyBindAction _capturingAction;
        private TextMeshProUGUI _capturingKeyText;

        private const float BindingRowHeight = 50f;
        private const float BindingRowSpacing = 6f;

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            // FindChildComponent 基于 transform.Find（不递归），控件均位于 m_rect_ContentRoot 下，必须写完整路径。
            _sensitivitySlider = FindChildComponent<Slider>("m_rect_ContentRoot/m_panel_General/m_slider_Sensitivity");
            _sensitivityValueText = FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_General/m_text_SensitivityValue");
            _scopeSensitivitySlider = FindChildComponent<Slider>("m_rect_ContentRoot/m_panel_General/m_slider_ScopeSensitivity");
            _scopeSensitivityValueText = FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_General/m_text_ScopeSensitivityValue");
            _sniperAimModeToggle = FindChildComponent<Toggle>("m_rect_ContentRoot/m_panel_General/m_toggle_SniperAimMode");
            _closeButton = FindChildComponent<Button>("m_rect_ContentRoot/m_panel_General/m_btn_Close");
            _returnMainMenuButton = FindChildComponent<Button>("m_rect_ContentRoot/m_panel_General/m_btn_ReturnMainMenu");

            _crosshairStyleButton = FindChildComponent<Button>("m_rect_ContentRoot/m_panel_General/m_btn_CrosshairStyle");
            _crosshairStyleText = FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_General/m_btn_CrosshairStyle/m_text_Label");
            _crosshairPreview = FindChildComponent<Image>("m_rect_ContentRoot/m_panel_General/m_img_CrosshairPreview");
            for (int i = 0; i < _crosshairColorButtons.Length; i++)
            {
                _crosshairColorButtons[i] = FindChildComponent<Button>($"m_rect_ContentRoot/m_panel_General/m_btn_Color{i}");
            }

            _tabGeneralButton = FindChildComponent<Button>("m_rect_ContentRoot/m_btn_TabGeneral");
            _tabInputButton = FindChildComponent<Button>("m_rect_ContentRoot/m_btn_TabInput");
            var panelGeneral = FindChildComponent<RectTransform>("m_rect_ContentRoot/m_panel_General");
            _panelGeneral = panelGeneral != null ? panelGeneral.gameObject : null;
            var panelInput = FindChildComponent<RectTransform>("m_rect_ContentRoot/m_panel_Input");
            _panelInput = panelInput != null ? panelInput.gameObject : null;

            _bindingHintText = FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_Input/m_text_BindingHint");
            _bindingListRoot = FindChildComponent<RectTransform>("m_rect_ContentRoot/m_panel_Input/m_rect_BindingList");
            _bindingRowTemplate = FindChildComponent<RectTransform>("m_rect_ContentRoot/m_panel_Input/m_rect_BindingList/m_rect_BindingRowTemplate");
            _resetBindingsButton = FindChildComponent<Button>("m_rect_ContentRoot/m_panel_Input/m_btn_ResetBindings");
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            CursorManager.Instance?.ShowCursor();
            // 与背包/开箱/纸条一致：打开设置（显示系统光标）时隐藏游戏准星，避免双光标并存
            CrosshairUpdater.Instance?.SetVisible(false);
            InitializeSensitivity();
            if (_sniperAimModeToggle != null)
            {
                _sniperAimModeToggle.isOn = SniperAimModeSetting.IsToggle;
            }

            BuildBindingRows();
            SetBindingHint("Click a key button, then press a new key. ESC to cancel.");
            CrosshairSetting.OnChanged += UpdateCrosshairViews;
            UpdateCrosshairViews();
            ShowTab(true);
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
            if (_returnMainMenuButton != null)
            {
                _returnMainMenuButton.onClick.RemoveAllListeners();
                _returnMainMenuButton.onClick.AddListener(OnReturnMainMenuClicked);
            }
            if (_tabGeneralButton != null)
            {
                _tabGeneralButton.onClick.RemoveAllListeners();
                _tabGeneralButton.onClick.AddListener(() => ShowTab(true));
            }
            if (_tabInputButton != null)
            {
                _tabInputButton.onClick.RemoveAllListeners();
                _tabInputButton.onClick.AddListener(() => ShowTab(false));
            }
            if (_resetBindingsButton != null)
            {
                _resetBindingsButton.onClick.RemoveAllListeners();
                _resetBindingsButton.onClick.AddListener(OnResetBindingsClicked);
            }
            if (_crosshairStyleButton != null)
            {
                _crosshairStyleButton.onClick.RemoveAllListeners();
                // 样式/颜色视图由 CrosshairSetting.OnChanged 统一刷新，这里只负责切样式
                _crosshairStyleButton.onClick.AddListener(() => CrosshairSetting.CycleStyle());
            }
            for (int i = 0; i < _crosshairColorButtons.Length; i++)
            {
                var button = _crosshairColorButtons[i];
                if (button == null) continue;
                // 闭包捕获当前索引，避免循环变量陷阱
                var capturedIndex = i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => CrosshairSetting.Color = CrosshairSetting.PresetColors[capturedIndex]);
            }
        }

        protected override void OnDestroy()
        {
            CancelCapture();
            CrosshairSetting.OnChanged -= UpdateCrosshairViews;
            DestroyPreviewSprite();
            CrosshairUpdater.Instance?.SetVisible(true);
            CursorManager.Instance?.HideCursor();
            SensitivitySetting.Save();
            SniperAimModeSetting.Save();
            KeyBindingSetting.Save();
            CrosshairSetting.Save();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (_isCapturing)
            {
                PollCapture();
            }
        }

        // ---- 页签切换 ----

        private void ShowTab(bool general)
        {
            // 切页签时取消进行中的改绑，避免在不可见面板上挂着"按任意键"状态
            CancelCapture();
            if (_panelGeneral != null)
            {
                _panelGeneral.SetActive(general);
            }
            if (_panelInput != null)
            {
                _panelInput.SetActive(!general);
            }
        }

        // ---- General 页签：灵敏度 ----

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

        // ---- General 页签：准星 ----

        /// <summary>
        /// 准星设置变动回调：刷新样式名文本与可视化预览。
        /// </summary>
        private void UpdateCrosshairViews()
        {
            if (_crosshairStyleText != null)
            {
                _crosshairStyleText.text = CrosshairSetting.GetStyleDisplayName(CrosshairSetting.Style);
            }
            if (_crosshairPreview != null)
            {
                DestroyPreviewSprite();
                _previewSprite = CrosshairSpriteFactory.Create(CrosshairSetting.Style, 64, 4f);
                _crosshairPreview.sprite = _previewSprite;
                _crosshairPreview.color = CrosshairSetting.Color;
            }
        }

        /// <summary>
        /// 销毁旧预览精灵及其贴图，避免每次变动泄漏一张 64x64 纹理。
        /// </summary>
        private void DestroyPreviewSprite()
        {
            if (_previewSprite == null) return;
            Object.Destroy(_previewSprite.texture);
            Object.Destroy(_previewSprite);
            _previewSprite = null;
        }

        // ---- Input 页签：按键改绑 ----

        /// <summary>
        /// 按 KeyBindAction 枚举顺序克隆行模板生成绑定列表。
        /// </summary>
        private void BuildBindingRows()
        {
            if (_bindingRowTemplate == null || _bindingListRoot == null) return;

            // 清理旧的克隆行（保留模板自身）
            for (int i = _bindingListRoot.childCount - 1; i >= 0; i--)
            {
                var child = _bindingListRoot.GetChild(i);
                if (child == _bindingRowTemplate) continue;
                Object.Destroy(child.gameObject);
            }

            var actions = KeyBindingSetting.Actions;
            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                var row = Object.Instantiate(_bindingRowTemplate.gameObject, _bindingListRoot);
                row.name = $"m_rect_BindingRow_{action}";
                row.SetActive(true);

                var rowRect = row.transform as RectTransform;
                if (rowRect != null)
                {
                    rowRect.anchoredPosition = new Vector2(0f, -i * (BindingRowHeight + BindingRowSpacing));
                }

                var label = row.transform.Find("m_text_ActionLabel")?.GetComponent<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = KeyBindingSetting.GetDisplayName(action);
                }

                var button = row.transform.Find("m_btn_Rebind")?.GetComponent<Button>();
                var keyText = button != null
                    ? button.transform.Find("m_text_Key")?.GetComponent<TextMeshProUGUI>()
                    : null;
                if (keyText != null)
                {
                    keyText.text = KeyBindingSetting.GetKeyDisplayName(KeyBindingSetting.GetKey(action));
                }
                if (button != null)
                {
                    // 闭包捕获当前动作与文本，避免循环变量陷阱
                    var capturedAction = action;
                    var capturedText = keyText;
                    button.onClick.AddListener(() => StartCapture(capturedAction, capturedText));
                }
            }
        }

        private void OnResetBindingsClicked()
        {
            CancelCapture();
            KeyBindingSetting.ResetToDefaults();
            BuildBindingRows();
            SetBindingHint("Bindings reset to defaults.");
        }

        private void StartCapture(KeyBindAction action, TextMeshProUGUI keyText)
        {
            // 同帧防抖：绑定鼠标键（如 LMB）时按下完成绑定、松开落在按钮上会再次触发 onClick，
            // 若不拦截会立刻重新进入捕获，表现为"重复选取且绑不上 LMB"
            if (Time.frameCount == _captureEndFrame) return;

            CancelCapture();
            _isCapturing = true;
            IsCapturingKey = true;
            _capturingAction = action;
            _capturingKeyText = keyText;
            if (_capturingKeyText != null)
            {
                _capturingKeyText.text = "...";
            }
            SetBindingHint($"Press a key for {KeyBindingSetting.GetDisplayName(action)} (ESC to cancel)");
        }

        private void PollCapture()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelCapture();
                SetBindingHint("Rebind cancelled.");
                return;
            }

            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (key == KeyCode.Escape || !Input.GetKeyDown(key)) continue;

                // 冲突检测：该键已绑给其他动作时拒绝并提示
                if (KeyBindingSetting.TryGetActionByKey(key, out var usedBy) && usedBy != _capturingAction)
                {
                    CancelCapture();
                    SetBindingHint($"{KeyBindingSetting.GetKeyDisplayName(key)} is already bound to {KeyBindingSetting.GetDisplayName(usedBy)}.");
                    return;
                }

                KeyBindingSetting.SetKey(_capturingAction, key);
                EndCapture();
                SetBindingHint($"{KeyBindingSetting.GetDisplayName(_capturingAction)} bound to {KeyBindingSetting.GetKeyDisplayName(key)}.");
                return;
            }
        }

        private void EndCapture()
        {
            if (_capturingKeyText != null)
            {
                _capturingKeyText.text = KeyBindingSetting.GetKeyDisplayName(KeyBindingSetting.GetKey(_capturingAction));
            }
            _isCapturing = false;
            IsCapturingKey = false;
            _capturingKeyText = null;
            _captureEndFrame = Time.frameCount;
        }

        private void CancelCapture()
        {
            if (!_isCapturing)
            {
                IsCapturingKey = false;
                return;
            }
            // 恢复显示当前生效的键位
            if (_capturingKeyText != null)
            {
                _capturingKeyText.text = KeyBindingSetting.GetKeyDisplayName(KeyBindingSetting.GetKey(_capturingAction));
            }
            _isCapturing = false;
            IsCapturingKey = false;
            _capturingKeyText = null;
            _captureEndFrame = Time.frameCount;
        }

        private void SetBindingHint(string text)
        {
            if (_bindingHintText != null)
            {
                _bindingHintText.text = text;
            }
        }

        /// <summary>
        /// 返回主界面：ChangeProcedure 内部会 CloseAll 并复位暂停状态，
        /// 无需手动先关设置；已在主菜单（从主菜单打开设置）时仅关闭本窗口，避免流程重进。
        /// </summary>
        private void OnReturnMainMenuClicked()
        {
            if (GameApp.IsCurrentProcedure<ProcedureMainMenu>())
            {
                GameModule.UI.CloseUI<SettingsUI>();
                return;
            }
            GameApp.ChangeProcedure<ProcedureMainMenu>();
        }
    }
}
