using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 准星样式。
    /// </summary>
    public enum CrosshairStyle
    {
        Dot,
        Cross,
        Circle,
        TShape,
        Reloading,
    }

    /// <summary>
    /// 战斗主 UI（HUD）。
    /// 显示玩家 HP、弹药、当前武器信息，以及可切换样式的鼠标跟随准星。
    /// </summary>
    [Window(UILayer.UI, location: "BattleMainUI")]
    public class BattleMainUI : UIWindow
    {
        #region 脚本工具生成的代码
        private TextMeshProUGUI _textHp;
        private TextMeshProUGUI _textAmmo;
        private TextMeshProUGUI _textWeapon;
        private Slider _sliderHp;
        private Slider _sliderStamina;
        private RectTransform _rectCrosshair;
        private TextMeshProUGUI _textExtraction;



        protected override void ScriptGenerator()
        {
            _textHp = FindChildComponent<TextMeshProUGUI>("m_rect_HudRoot/m_text_Hp");
            _textAmmo = FindChildComponent<TextMeshProUGUI>("m_rect_HudRoot/m_text_Ammo");
            _textWeapon = FindChildComponent<TextMeshProUGUI>("m_rect_HudRoot/m_text_Weapon");
            _sliderHp = FindChildComponent<Slider>("m_rect_HudRoot/m_slider_Hp");
            _sliderStamina = FindChildComponent<Slider>("m_rect_HudRoot/m_slider_Stamina");
            _rectCrosshair = FindChildComponent<RectTransform>("m_rect_Crosshair");
            _textExtraction = FindChildComponent<TextMeshProUGUI>("m_rect_HudRoot/m_text_Extraction");
        }
        #endregion

        [Header("准星")]
        [SerializeField] private int _crosshairSize = 24;
        [SerializeField] private float _crosshairThickness = 3f;
        [SerializeField] private float _reloadingSpinSpeed = 360f;

        private bool _pendingFirstFrameRefresh = true;
        private const int READY_POLL_FRAMES = 60;
        private int _readyPollFrames = READY_POLL_FRAMES;
        private Image _crosshairImage;
        private CrosshairStyle _currentStyle;
        private CrosshairStyle _preReloadStyle;
        private CrosshairUpdater _crosshairUpdater;
        private bool _isReloading;
        private readonly System.Collections.Generic.Dictionary<CrosshairStyle, Sprite> _crosshairSprites = new();

        public bool IsReloading => _isReloading;
        public float ReloadingSpinSpeed => _reloadingSpinSpeed;

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            InitializeCrosshair();
            RefreshAll();
        }

        protected override void OnDestroy()
        {
            CrosshairSetting.OnChanged -= ApplyCrosshairSetting;
            base.OnDestroy();
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent<int, int>(IPlayerEvent_Event.OnHpChanged, OnHpChanged);
            AddUIEvent<int, int>(IPlayerEvent_Event.OnAmmoChanged, OnAmmoChanged);
            AddUIEvent<int, int, int>(IWeaponEvent_Event.OnWeaponEquipped, OnWeaponEquipped);
            AddUIEvent<int, int>(IWeaponEvent_Event.OnWeaponSwitched, OnWeaponSwitched);
            AddUIEvent<int, bool>(IWeaponEvent_Event.OnReloadStateChanged, OnReloadStateChanged);
            AddUIEvent<int, int>(IPlayerEvent_Event.OnStaminaChanged, OnStaminaChanged);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (_pendingFirstFrameRefresh)
            {
                _pendingFirstFrameRefresh = false;
                RefreshAll();
            }

            // 系统组件（PlayerSystem / WeaponSystem 等）的 Start 可能在 UI 创建之后才完成，
            // 持续轮询几帧，确保玩家与武器信息正确显示，避免一直显示 -/-。
            if (_readyPollFrames > 0 && (PlayerSystem.Instance == null || WeaponSystem.Instance?.CurrentWeapon == null))
            {
                _readyPollFrames--;
                RefreshAll();
            }

            UpdateExtractionCountdown();
        }

        #region 撤离倒计时

        private static readonly Color ExtractionNormalColor = new Color(0.55f, 1f, 0.6f);
        private static readonly Color ExtractionPausedColor = new Color(1f, 0.45f, 0.35f);

        // 倒计时显示缓存：避免每帧字符串插值分配与 TMP 置脏
        private int _extractionShownTenths = -1; // 已显示的 0.1s 量化剩余时间，-1 表示未显示
        private bool _extractionPauseShown;      // 当前是否正在显示暂停文本

        /// <summary>
        /// 撤离倒计时显示（屏幕顶部居中）：拉取 <see cref="ExtractionSystem"/> 状态，
        /// 圈内显示剩余秒数；圈内有敌人时暂停并红字提示。
        /// </summary>
        private void UpdateExtractionCountdown()
        {
            if (_textExtraction == null) return;

            var extraction = ExtractionSystem.Instance;
            if (extraction != null && extraction.IsCountingDown)
            {
                if (!_textExtraction.gameObject.activeSelf)
                {
                    _textExtraction.gameObject.SetActive(true);
                }
                if (extraction.IsPaused)
                {
                    // 暂停状态没变就不重赋值
                    if (!_extractionPauseShown)
                    {
                        _extractionPauseShown = true;
                        _extractionShownTenths = -1;
                        _textExtraction.text = Loc.Get("ui.battle.extraction_paused");
                        _textExtraction.color = ExtractionPausedColor;
                    }
                }
                else
                {
                    // 剩余时间按 0.1s 量化，显示值没变就不重赋值
                    int tenths = Mathf.RoundToInt(extraction.RemainingSeconds * 10f);
                    if (_extractionPauseShown || tenths != _extractionShownTenths)
                    {
                        _extractionPauseShown = false;
                        _extractionShownTenths = tenths;
                        _textExtraction.text = Loc.Get("ui.battle.extracting", $"{tenths / 10f:F1}");
                        _textExtraction.color = ExtractionNormalColor;
                    }
                }
            }
            else if (_textExtraction.gameObject.activeSelf)
            {
                _textExtraction.gameObject.SetActive(false);
                _extractionPauseShown = false;
                _extractionShownTenths = -1;
            }
        }

        #endregion

        #region 准星

        /// <summary>
        /// 初始化准星显示组件与样式资源。
        /// </summary>
        private void InitializeCrosshair()
        {
            if (_rectCrosshair == null) return;

            _crosshairImage = _rectCrosshair.GetComponent<Image>();
            if (_crosshairImage == null)
            {
                _crosshairImage = _rectCrosshair.gameObject.AddComponent<Image>();
            }

            _rectCrosshair.sizeDelta = new Vector2(_crosshairSize, _crosshairSize);

            GenerateAllCrosshairSprites();
            SetCrosshairStyle(CrosshairSetting.Style);
            CrosshairSetting.OnChanged += ApplyCrosshairSetting;

            _crosshairUpdater = _rectCrosshair.gameObject.GetComponent<CrosshairUpdater>();
            if (_crosshairUpdater == null)
            {
                _crosshairUpdater = _rectCrosshair.gameObject.AddComponent<CrosshairUpdater>();
            }
            _crosshairUpdater.Initialize(this, _rectCrosshair, Canvas);
        }

        /// <summary>
        /// 循环切换到下一个准星样式（换弹中不响应）。
        /// 实际切换由 CrosshairSetting 写档 + OnChanged 广播完成，本窗口在 ApplyCrosshairSetting 中应用。
        /// </summary>
        public void CycleCrosshairStyle()
        {
            if (_isReloading) return;
            CrosshairSetting.CycleStyle();
        }

        /// <summary>
        /// 设置变动回调：应用存档中的样式与颜色。
        /// 换弹中的转圈样式不被覆盖，仅更新颜色，样式待换弹结束后由 _preReloadStyle 恢复。
        /// </summary>
        private void ApplyCrosshairSetting()
        {
            if (_isReloading)
            {
                if (_crosshairImage != null)
                {
                    _crosshairImage.color = CrosshairSetting.Color;
                }
                return;
            }
            SetCrosshairStyle(CrosshairSetting.Style);
        }

        /// <summary>
        /// 设置当前准星样式（样式精灵为白色，颜色统一由 CrosshairSetting 染色）。
        /// </summary>
        public void SetCrosshairStyle(CrosshairStyle style)
        {
            _currentStyle = style;
            if (_crosshairImage != null && _crosshairSprites.TryGetValue(style, out var sprite))
            {
                _crosshairImage.sprite = sprite;
                _crosshairImage.color = CrosshairSetting.Color;
            }
        }

        /// <summary>
        /// 开/关狙击镜时隐藏/显示普通准星图案（只关 Image，不动 GameObject——
        /// CrosshairUpdater 要持续累加鼠标位移来驱动镜窗位置）。
        /// </summary>
        public void SetCrosshairImageHidden(bool hidden)
        {
            if (_crosshairImage != null)
            {
                _crosshairImage.enabled = !hidden;
            }
        }

        /// <summary>
        /// 预生成所有准星样式的 Sprite（白色贴图，由 CrosshairSpriteFactory 统一绘制）。
        /// </summary>
        private void GenerateAllCrosshairSprites()
        {
            foreach (CrosshairStyle style in System.Enum.GetValues(typeof(CrosshairStyle)))
            {
                _crosshairSprites[style] = CrosshairSpriteFactory.Create(style, _crosshairSize, _crosshairThickness);
            }
        }

        #endregion

        #region 事件回调

        private void OnHpChanged(int currentHp, int maxHp)
        {
            if (_textHp != null) _textHp.text = Loc.Get("ui.battle.hp", currentHp, maxHp);
            if (_sliderHp != null)
                _sliderHp.value = maxHp > 0 ? (float)currentHp / maxHp : 0f;
        }

        private void OnStaminaChanged(int currentStamina, int maxStamina)
        {
            if (_sliderStamina != null)
                _sliderStamina.value = maxStamina > 0 ? (float)currentStamina / maxStamina : 0f;
        }

        private void OnAmmoChanged(int currentAmmo, int maxAmmo)
        {
            if (_textAmmo != null) _textAmmo.text = Loc.Get("ui.battle.ammo", currentAmmo, maxAmmo);
        }

        private void OnWeaponEquipped(int ownerId, int slot, int weaponConfigId)
        {
            RefreshAll();
        }

        private void OnWeaponSwitched(int ownerId, int slot)
        {
            RefreshAll();
        }

        private void OnReloadStateChanged(int ownerId, bool isReloading)
        {
            _isReloading = isReloading;

            if (isReloading)
            {
                if (_currentStyle != CrosshairStyle.Reloading)
                {
                    _preReloadStyle = _currentStyle;
                }
                SetCrosshairStyle(CrosshairStyle.Reloading);
            }
            else
            {
                SetCrosshairStyle(_preReloadStyle);
            }
        }

        #endregion



        private void RefreshAll()
        {
            var playerSystem = PlayerSystem.Instance;
            if (playerSystem != null)
            {
                if (_textHp != null) _textHp.text = Loc.Get("ui.battle.hp", playerSystem.CurrentHp, playerSystem.MaxHp);
                if (_sliderHp != null) _sliderHp.value = playerSystem.MaxHp > 0 ? (float)playerSystem.CurrentHp / playerSystem.MaxHp : 0f;
                if (_sliderStamina != null) _sliderStamina.value = playerSystem.MaxStamina > 0 ? (float)playerSystem.CurrentStamina / playerSystem.MaxStamina : 0f;
            }
            else
            {
                if (_textHp != null) _textHp.text = Loc.Get("ui.battle.hp", "-", "-");
                if (_sliderHp != null) _sliderHp.value = 0f;
                if (_sliderStamina != null) _sliderStamina.value = 0f;
            }

            var weapon = WeaponSystem.Instance?.CurrentWeapon;
            if (weapon != null)
            {
                if (_textAmmo != null) _textAmmo.text = Loc.Get("ui.battle.ammo", weapon.CurrentAmmo, weapon.Config.clipSize);
                if (_textWeapon != null) _textWeapon.text = Loc.Get("ui.battle.weapon", weapon.Config.name);
            }
            else
            {
                if (_textAmmo != null) _textAmmo.text = Loc.Get("ui.battle.ammo", "-", "-");
                if (_textWeapon != null) _textWeapon.text = Loc.Get("ui.battle.weapon", "-");
            }

        }

    }
}
