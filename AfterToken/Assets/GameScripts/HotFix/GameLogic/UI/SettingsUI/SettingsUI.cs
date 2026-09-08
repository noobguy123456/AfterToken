using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 设置面板。
    /// General 页签：灵敏度/开镜灵敏度/狙击开镜模式/准星样式颜色/语言/返回主界面。
    /// Audio 页签：主音量/音乐/音效滑条（VolumeSetting，写 SaveSystem 并即时应用 AudioModule）。
    /// Graphics 页签：画质档位左右切换（QualitySetting，写 SaveSystem 并即时应用 QualitySettings）。
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
        private enum SettingsTab
        {
            General,
            Audio,
            Graphics,
            Input,
            AI,
        }

        /// <summary>页签常态/选中色（重构后的左侧页签栏高亮）。</summary>
        private static readonly Color TabNormalColor = new Color(0.14f, 0.16f, 0.19f, 1f);
        private static readonly Color TabSelectedColor = new Color(0.16f, 0.55f, 0.38f, 1f);

        private Button _tabGeneralButton;
        private Button _tabAudioButton;
        private Button _tabGraphicsButton;
        private Button _tabInputButton;
        private Button _tabAIButton;
        private GameObject _panelGeneral;
        private GameObject _panelAudio;
        private GameObject _panelGraphics;
        private GameObject _panelInput;
        private GameObject _panelAI;

        // ---- AI 页签：LLM 配置 ----
        private TMP_InputField _llmEndpointInput;
        private TMP_InputField _llmApiKeyInput;
        private TMP_InputField _llmModelInput;
        private Toggle _llmControlModeToggle;
        private Button _llmSaveButton;
        private TextMeshProUGUI _llmStatusText;

        // ---- Audio 页签：音量 ----
        private Slider _masterVolumeSlider;
        private TextMeshProUGUI _masterVolumeValueText;
        private Slider _musicVolumeSlider;
        private TextMeshProUGUI _musicVolumeValueText;
        private Slider _soundVolumeSlider;
        private TextMeshProUGUI _soundVolumeValueText;

        // ---- Graphics 页签：画质 ----
        private Button _qualityPrevButton;
        private Button _qualityNextButton;
        private TextMeshProUGUI _qualityValueText;

        // ---- General 页签：准星 ----
        private Button _crosshairStyleButton;
        private TextMeshProUGUI _crosshairStyleText;
        private Image _crosshairPreview;
        private Sprite _previewSprite;
        private readonly Button[] _crosshairColorButtons = new Button[CrosshairSetting.PresetColors.Length];

        // ---- General 页签：语言（运行时克隆生成的循环切换按钮） ----
        private Button _languageButton;
        private TextMeshProUGUI _languageButtonText;

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

        private const float BindingRowHeight = 44f;
        private const float BindingRowSpacing = 4f;

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            // FindChildComponent 基于 transform.Find（不递归），控件均位于 m_rect_ContentRoot 下，必须写完整路径。
            _sensitivitySlider = FindChildComponent<Slider>("m_rect_ContentRoot/m_panel_General/m_slider_Sensitivity");
            _sensitivityValueText = FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_General/m_text_SensitivityValue");
            _scopeSensitivitySlider = FindChildComponent<Slider>("m_rect_ContentRoot/m_panel_General/m_slider_ScopeSensitivity");
            _scopeSensitivityValueText = FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_General/m_text_ScopeSensitivityValue");
            _sniperAimModeToggle = FindChildComponent<Toggle>("m_rect_ContentRoot/m_panel_General/m_toggle_SniperAimMode");
            _closeButton = FindChildComponent<Button>("m_rect_ContentRoot/m_btn_Close");
            _returnMainMenuButton = FindChildComponent<Button>("m_rect_ContentRoot/m_btn_ReturnMainMenu");

            _crosshairStyleButton = FindChildComponent<Button>("m_rect_ContentRoot/m_panel_General/m_btn_CrosshairStyle");
            _crosshairStyleText = FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_General/m_btn_CrosshairStyle/m_text_Label");
            _crosshairPreview = FindChildComponent<Image>("m_rect_ContentRoot/m_panel_General/m_img_CrosshairPreview");
            for (int i = 0; i < _crosshairColorButtons.Length; i++)
            {
                _crosshairColorButtons[i] = FindChildComponent<Button>($"m_rect_ContentRoot/m_panel_General/m_btn_Color{i}");
            }

            _tabGeneralButton = FindChildComponent<Button>("m_rect_ContentRoot/m_btn_TabGeneral");
            _tabAudioButton = FindChildComponent<Button>("m_rect_ContentRoot/m_btn_TabAudio");
            _tabGraphicsButton = FindChildComponent<Button>("m_rect_ContentRoot/m_btn_TabGraphics");
            _tabInputButton = FindChildComponent<Button>("m_rect_ContentRoot/m_btn_TabInput");
            _tabAIButton = FindChildComponent<Button>("m_rect_ContentRoot/m_btn_TabAI");
            _languageButton = FindChildComponent<Button>("m_rect_ContentRoot/m_panel_General/m_btn_Language");
            if (_languageButton != null)
            {
                _languageButtonText = FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_General/m_btn_Language/m_text_Label");
            }
            var panelGeneral = FindChildComponent<RectTransform>("m_rect_ContentRoot/m_panel_General");
            _panelGeneral = panelGeneral != null ? panelGeneral.gameObject : null;
            var panelAudio = FindChildComponent<RectTransform>("m_rect_ContentRoot/m_panel_Audio");
            _panelAudio = panelAudio != null ? panelAudio.gameObject : null;
            var panelGraphics = FindChildComponent<RectTransform>("m_rect_ContentRoot/m_panel_Graphics");
            _panelGraphics = panelGraphics != null ? panelGraphics.gameObject : null;
            var panelInput = FindChildComponent<RectTransform>("m_rect_ContentRoot/m_panel_Input");
            _panelInput = panelInput != null ? panelInput.gameObject : null;
            var panelAI = FindChildComponent<RectTransform>("m_rect_ContentRoot/m_panel_AI");
            _panelAI = panelAI != null ? panelAI.gameObject : null;

            _llmEndpointInput = FindChildComponent<TMP_InputField>("m_rect_ContentRoot/m_panel_AI/m_input_LlmEndpoint");
            _llmApiKeyInput = FindChildComponent<TMP_InputField>("m_rect_ContentRoot/m_panel_AI/m_input_LlmApiKey");
            _llmModelInput = FindChildComponent<TMP_InputField>("m_rect_ContentRoot/m_panel_AI/m_input_LlmModel");
            _llmControlModeToggle = FindChildComponent<Toggle>("m_rect_ContentRoot/m_panel_AI/m_toggle_LlmControlMode");
            _llmSaveButton = FindChildComponent<Button>("m_rect_ContentRoot/m_panel_AI/m_btn_LlmSave");
            _llmStatusText = FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_AI/m_text_LlmStatus");

            _masterVolumeSlider = FindChildComponent<Slider>("m_rect_ContentRoot/m_panel_Audio/m_slider_MasterVolume");
            _masterVolumeValueText = FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_Audio/m_text_MasterVolumeValue");
            _musicVolumeSlider = FindChildComponent<Slider>("m_rect_ContentRoot/m_panel_Audio/m_slider_MusicVolume");
            _musicVolumeValueText = FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_Audio/m_text_MusicVolumeValue");
            _soundVolumeSlider = FindChildComponent<Slider>("m_rect_ContentRoot/m_panel_Audio/m_slider_SoundVolume");
            _soundVolumeValueText = FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_Audio/m_text_SoundVolumeValue");

            _qualityPrevButton = FindChildComponent<Button>("m_rect_ContentRoot/m_panel_Graphics/m_btn_QualityPrev");
            _qualityNextButton = FindChildComponent<Button>("m_rect_ContentRoot/m_panel_Graphics/m_btn_QualityNext");
            _qualityValueText = FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_Graphics/m_text_QualityValue");

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

            BindStaticTexts();
            UpdateLanguageView();
            UpdateSniperAimModeText();
            LocalizationSystem.Instance.OnLanguageChanged += UpdateLanguageView;
            LocalizationSystem.Instance.OnLanguageChanged += UpdateSniperAimModeText;
            BuildBindingRows();
            SetBindingHint(Loc.Get("ui.settings.rebind_hint"));
            CrosshairSetting.OnChanged += UpdateCrosshairViews;
            UpdateCrosshairViews();
            InitializeVolume();
            UpdateQualityView();
            InitializeLlmPanel();
            ShowTab(SettingsTab.General);
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
                _sniperAimModeToggle.onValueChanged.AddListener(isOn =>
                {
                    SniperAimModeSetting.IsToggle = isOn;
                    UpdateSniperAimModeText();
                });
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
                _tabGeneralButton.onClick.AddListener(() => ShowTab(SettingsTab.General));
            }
            if (_tabAudioButton != null)
            {
                _tabAudioButton.onClick.RemoveAllListeners();
                _tabAudioButton.onClick.AddListener(() => ShowTab(SettingsTab.Audio));
            }
            if (_tabGraphicsButton != null)
            {
                _tabGraphicsButton.onClick.RemoveAllListeners();
                _tabGraphicsButton.onClick.AddListener(() => ShowTab(SettingsTab.Graphics));
            }
            if (_tabInputButton != null)
            {
                _tabInputButton.onClick.RemoveAllListeners();
                _tabInputButton.onClick.AddListener(() => ShowTab(SettingsTab.Input));
            }
            if (_tabAIButton != null)
            {
                _tabAIButton.onClick.RemoveAllListeners();
                _tabAIButton.onClick.AddListener(() => ShowTab(SettingsTab.AI));
            }
            if (_llmSaveButton != null)
            {
                _llmSaveButton.onClick.RemoveAllListeners();
                _llmSaveButton.onClick.AddListener(OnLlmSaveClicked);
            }
            if (_llmControlModeToggle != null)
            {
                _llmControlModeToggle.onValueChanged.RemoveAllListeners();
                _llmControlModeToggle.onValueChanged.AddListener(_ => UpdateLlmControlModeText());
            }
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.onValueChanged.RemoveAllListeners();
                _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.onValueChanged.RemoveAllListeners();
                _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }
            if (_soundVolumeSlider != null)
            {
                _soundVolumeSlider.onValueChanged.RemoveAllListeners();
                _soundVolumeSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
            }
            if (_qualityPrevButton != null)
            {
                _qualityPrevButton.onClick.RemoveAllListeners();
                _qualityPrevButton.onClick.AddListener(() => CycleQuality(-1));
            }
            if (_qualityNextButton != null)
            {
                _qualityNextButton.onClick.RemoveAllListeners();
                _qualityNextButton.onClick.AddListener(() => CycleQuality(1));
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
            if (_languageButton != null)
            {
                _languageButton.onClick.RemoveAllListeners();
                _languageButton.onClick.AddListener(CycleLanguage);
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
            LocalizationSystem.Instance.OnLanguageChanged -= UpdateLanguageView;
            LocalizationSystem.Instance.OnLanguageChanged -= UpdateSniperAimModeText;
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

        private void ShowTab(SettingsTab tab)
        {
            // 切页签时取消进行中的改绑，避免在不可见面板上挂着"按任意键"状态
            CancelCapture();
            if (_panelGeneral != null)
            {
                _panelGeneral.SetActive(tab == SettingsTab.General);
            }
            if (_panelAudio != null)
            {
                _panelAudio.SetActive(tab == SettingsTab.Audio);
            }
            if (_panelGraphics != null)
            {
                _panelGraphics.SetActive(tab == SettingsTab.Graphics);
            }
            if (_panelInput != null)
            {
                _panelInput.SetActive(tab == SettingsTab.Input);
            }
            if (_panelAI != null)
            {
                _panelAI.SetActive(tab == SettingsTab.AI);
            }
            if (tab == SettingsTab.AI)
            {
                UpdateLlmStatusView();
            }
            UpdateTabHighlight(tab);
        }

        /// <summary>页签选中高亮：选中页签按钮染主题绿，其余回常态色。</summary>
        private void UpdateTabHighlight(SettingsTab tab)
        {
            SetTabColor(_tabGeneralButton, tab == SettingsTab.General);
            SetTabColor(_tabAudioButton, tab == SettingsTab.Audio);
            SetTabColor(_tabGraphicsButton, tab == SettingsTab.Graphics);
            SetTabColor(_tabInputButton, tab == SettingsTab.Input);
            SetTabColor(_tabAIButton, tab == SettingsTab.AI);
        }

        private static void SetTabColor(Button button, bool selected)
        {
            var image = button != null ? button.GetComponent<Image>() : null;
            if (image != null)
            {
                image.color = selected ? TabSelectedColor : TabNormalColor;
            }
        }

        // ---- AI 页签：LLM 配置 ----

        /// <summary>打开面板时回填当前配置；apiKey 输入框留空表示"不改动已有 key"。</summary>
        private void InitializeLlmPanel()
        {
            var config = AI.Llm.LlmConfig.Load();
            if (_llmEndpointInput != null)
            {
                _llmEndpointInput.text = config.endpoint ?? string.Empty;
            }
            if (_llmApiKeyInput != null)
            {
                _llmApiKeyInput.text = string.Empty;
                _llmApiKeyInput.contentType = TMP_InputField.ContentType.Password;
                // 已有 key 时用占位符提示"已保存，输入则覆盖"
                if (_llmApiKeyInput.placeholder is TextMeshProUGUI placeholder)
                {
                    placeholder.text = !string.IsNullOrEmpty(config.GetApiKey()) ? "********" : "sk-...";
                }
            }
            if (_llmModelInput != null)
            {
                _llmModelInput.text = config.model ?? string.Empty;
            }
            if (_llmControlModeToggle != null)
            {
                _llmControlModeToggle.isOn = config.IsLlmControl;
                UpdateLlmControlModeText();
            }
            UpdateLlmStatusView();
        }

        /// <summary>操控开关右侧文本显示当前模式（LLM/FSM），与狙击开镜开关同模式。</summary>
        private void UpdateLlmControlModeText()
        {
            if (_llmControlModeToggle == null) return;
            var label = _llmControlModeToggle.transform.Find("m_text_Label")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = _llmControlModeToggle.isOn ? "LLM" : "FSM";
            }
        }

        private void OnLlmSaveClicked()
        {
            var old = AI.Llm.LlmConfig.Load();
            var config = new AI.Llm.LlmConfig
            {
                endpoint = _llmEndpointInput != null ? _llmEndpointInput.text.Trim() : old.endpoint,
                // 输入框留空 = 保留旧 key
                apiKey = _llmApiKeyInput != null && !string.IsNullOrEmpty(_llmApiKeyInput.text)
                    ? _llmApiKeyInput.text.Trim()
                    : old.GetApiKey(),
                model = _llmModelInput != null ? _llmModelInput.text.Trim() : old.model,
                timeoutSeconds = old.timeoutSeconds,
                temperature = old.temperature,
                controlMode = _llmControlModeToggle != null && _llmControlModeToggle.isOn ? "llm" : "fsm",
            };

            bool saved = AI.Llm.LlmConfig.Save(config);
            if (saved && _llmApiKeyInput != null)
            {
                _llmApiKeyInput.text = string.Empty;
            }
            // 局内队友立即套用新配置
            CompanionSystem.Instance?.Brain?.ReloadConfig();
            UpdateLlmStatusView(saved
                ? Loc.Get("ui.settings.llm.saved")
                : Loc.Get("ui.settings.llm.save_failed"));
        }

        /// <summary>刷新链路状态行；message 非空时优先显示（保存反馈）。</summary>
        private void UpdateLlmStatusView(string message = null)
        {
            if (_llmStatusText == null)
            {
                return;
            }
            if (message != null)
            {
                _llmStatusText.text = message;
                return;
            }

            var config = AI.Llm.LlmConfig.Load();
            string configured = Loc.Get(config.IsValid ? "ui.settings.llm.configured" : "ui.settings.llm.not_configured");
            var brain = CompanionSystem.Instance?.Brain;
            if (brain == null)
            {
                // 不在局内没有链路状态，只显示是否已配置
                _llmStatusText.text = configured;
                return;
            }

            string linkKey = brain.LinkState switch
            {
                AI.Llm.CompanionLinkState.Online => "ui.settings.llm.status.online",
                AI.Llm.CompanionLinkState.Degraded => "ui.settings.llm.status.degraded",
                _ => "ui.settings.llm.status.offline",
            };
            _llmStatusText.text = $"{configured} | {Loc.Get(linkKey)}";
        }

        // ---- Audio 页签：音量 ----

        private void InitializeVolume()
        {
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.minValue = VolumeSetting.Min;
                _masterVolumeSlider.maxValue = VolumeSetting.Max;
                _masterVolumeSlider.wholeNumbers = false;
                _masterVolumeSlider.value = VolumeSetting.Master;
            }
            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.minValue = VolumeSetting.Min;
                _musicVolumeSlider.maxValue = VolumeSetting.Max;
                _musicVolumeSlider.wholeNumbers = false;
                _musicVolumeSlider.value = VolumeSetting.Music;
            }
            if (_soundVolumeSlider != null)
            {
                _soundVolumeSlider.minValue = VolumeSetting.Min;
                _soundVolumeSlider.maxValue = VolumeSetting.Max;
                _soundVolumeSlider.wholeNumbers = false;
                _soundVolumeSlider.value = VolumeSetting.Sound;
            }

            UpdateVolumeText(_masterVolumeValueText, VolumeSetting.Master);
            UpdateVolumeText(_musicVolumeValueText, VolumeSetting.Music);
            UpdateVolumeText(_soundVolumeValueText, VolumeSetting.Sound);
        }

        private void OnMasterVolumeChanged(float value)
        {
            VolumeSetting.Master = value;
            UpdateVolumeText(_masterVolumeValueText, value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            VolumeSetting.Music = value;
            UpdateVolumeText(_musicVolumeValueText, value);
        }

        private void OnSoundVolumeChanged(float value)
        {
            VolumeSetting.Sound = value;
            UpdateVolumeText(_soundVolumeValueText, value);
        }

        private void UpdateVolumeText(TextMeshProUGUI text, float value)
        {
            if (text != null)
            {
                // 行首标签已说明含义，数值文本只显示数字
                text.text = value.ToString("F2");
            }
        }

        // ---- Graphics 页签：画质 ----

        /// <summary>
        /// 左右箭头循环切换画质档位（越过边界时回绕）。
        /// </summary>
        private void CycleQuality(int delta)
        {
            int count = QualitySetting.LevelCount;
            if (count <= 0) return;
            int next = (QualitySetting.Level + delta + count) % count;
            QualitySetting.Level = next;
            UpdateQualityView();
        }

        private void UpdateQualityView()
        {
            if (_qualityValueText != null)
            {
                _qualityValueText.text = QualitySetting.GetLevelName(QualitySetting.Level);
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
                // 行首标签已说明含义，数值文本只显示数字
                _sensitivityValueText.text = value.ToString("F2");
            }
        }

        private void UpdateScopeSensitivityText(float value)
        {
            if (_scopeSensitivityValueText != null)
            {
                _scopeSensitivityValueText.text = value.ToString("F2");
            }
        }

        // ---- 多语言：静态文本绑定 + 语言切换 ----

        /// <summary>
        /// 把 prefab 上的硬编码静态文本绑定到词条，语言切换时自动刷新。
        /// </summary>
        private void BindStaticTexts()
        {
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_text_Title"), "ui.settings.title");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_btn_TabGeneral/m_text_Label"), "ui.settings.tab.general");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_btn_TabAudio/m_text_Label"), "ui.settings.tab.audio");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_btn_TabGraphics/m_text_Label"), "ui.settings.tab.graphics");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_btn_TabInput/m_text_Label"), "ui.settings.tab.input");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_btn_TabAI/m_text_Label"), "ui.settings.tab.ai");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_General/m_text_LanguageLabel"), "ui.settings.language");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_General/m_text_SensitivityLabel"), "ui.settings.crosshair_sensitivity");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_General/m_text_ScopeSensitivityLabel"), "ui.settings.scope_sensitivity");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_General/m_text_SniperAimLabel"), "ui.settings.sniper_aim_mode");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_General/m_text_CrosshairStyleLabel"), "ui.settings.crosshair_style");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_General/m_text_CrosshairColorLabel"), "ui.settings.crosshair_color");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_btn_ReturnMainMenu/m_text_Close"), "ui.common.main_menu");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_Input/m_btn_ResetBindings/m_text_Label"), "ui.settings.reset_defaults");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_Audio/m_text_MasterVolumeLabel"), "ui.settings.master_volume");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_Audio/m_text_MusicVolumeLabel"), "ui.settings.music_volume");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_Audio/m_text_SoundVolumeLabel"), "ui.settings.sound_volume");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_Graphics/m_text_QualityLabel"), "ui.settings.quality");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_AI/m_text_LlmEndpointLabel"), "ui.settings.llm.endpoint");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_AI/m_text_LlmApiKeyLabel"), "ui.settings.llm.apikey");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_AI/m_text_LlmModelLabel"), "ui.settings.llm.model");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_AI/m_text_LlmControlModeLabel"), "ui.settings.llm.control_mode");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_AI/m_text_LlmStatusLabel"), "ui.settings.llm.status");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_AI/m_btn_LlmSave/m_text_Label"), "ui.settings.llm.save");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_rect_ContentRoot/m_panel_AI/m_text_LlmHint"), "ui.settings.llm.hint");
        }

        /// <summary>
        /// 狙击开镜模式：开关右侧文本显示当前状态（Toggle/Hold）。
        /// </summary>
        private void UpdateSniperAimModeText()
        {
            if (_sniperAimModeToggle == null) return;
            var label = _sniperAimModeToggle.transform.Find("m_text_Label")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = Loc.Get(SniperAimModeSetting.IsToggle ? "ui.settings.aim_mode.toggle" : "ui.settings.aim_mode.hold");
            }
        }

        /// <summary>
        /// 循环切换支持的语言。
        /// </summary>
        private void CycleLanguage()
        {
            var langs = LocalizationSystem.SupportedLanguages;
            int index = System.Array.IndexOf(langs, LocalizationSystem.Instance.Current);
            index = (index + 1) % langs.Length;
            LocalizationSystem.Instance.SetLanguage(langs[index]);
        }

        /// <summary>
        /// 刷新语言按钮文本（显示语言自身的叫法，不走词条）。
        /// </summary>
        private void UpdateLanguageView()
        {
            if (_languageButtonText != null)
            {
                _languageButtonText.text = LocalizationSystem.GetLanguageDisplayName(LocalizationSystem.Instance.Current);
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
