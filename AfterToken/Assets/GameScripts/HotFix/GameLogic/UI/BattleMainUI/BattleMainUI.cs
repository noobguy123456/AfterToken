using UnityEngine;
using UnityEngine.UI;
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
    }

    /// <summary>
    /// 战斗主 UI（HUD）。
    /// 显示玩家 HP、弹药、当前武器信息，以及可切换样式的鼠标跟随准星。
    /// </summary>
    [Window(UILayer.UI, location: "BattleMainUI")]
    public class BattleMainUI : UIWindow
    {
        #region 脚本工具生成的代码
        private Text _textHp;
        private Text _textAmmo;
        private Text _textWeapon;
        private RectTransform _rectCrosshair;

        protected override void ScriptGenerator()
        {
            _textHp = FindChildComponent<Text>("m_rect_HudRoot/m_text_Hp");
            _textAmmo = FindChildComponent<Text>("m_rect_HudRoot/m_text_Ammo");
            _textWeapon = FindChildComponent<Text>("m_rect_HudRoot/m_text_Weapon");
            _rectCrosshair = FindChildComponent<RectTransform>("m_rect_Crosshair");
        }
        #endregion

        [Header("准星")]
        [SerializeField] private CrosshairStyle _defaultCrosshairStyle = CrosshairStyle.Cross;
        [SerializeField] private int _crosshairSize = 24;
        [SerializeField] private Color _crosshairColor = new Color(0.2f, 1f, 0.2f, 0.9f);
        [SerializeField] private float _crosshairThickness = 3f;

        private bool _pendingFirstFrameRefresh = true;
        private Image _crosshairImage;
        private CrosshairStyle _currentStyle;
        private CrosshairUpdater _crosshairUpdater;
        private readonly System.Collections.Generic.Dictionary<CrosshairStyle, Sprite> _crosshairSprites = new();

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            InitializeCrosshair();
            RefreshAll();
            Log.Debug($"[BattleMainUI] 节点绑定: Hp={_textHp != null}, Ammo={_textAmmo != null}, Weapon={_textWeapon != null}, Crosshair={_rectCrosshair != null}");
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent<int, int>(IPlayerEvent_Event.OnHpChanged, OnHpChanged);
            AddUIEvent<int, int>(IPlayerEvent_Event.OnAmmoChanged, OnAmmoChanged);
            AddUIEvent<int, int, int>(IWeaponEvent_Event.OnWeaponEquipped, OnWeaponEquipped);
            AddUIEvent<int, int>(IWeaponEvent_Event.OnWeaponSwitched, OnWeaponSwitched);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (_pendingFirstFrameRefresh)
            {
                _pendingFirstFrameRefresh = false;
                RefreshAll();
                Log.Debug("[BattleMainUI] 首帧延迟刷新完成");
            }
        }

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
            SetCrosshairStyle(_defaultCrosshairStyle);

            _crosshairUpdater = _rectCrosshair.gameObject.GetComponent<CrosshairUpdater>();
            if (_crosshairUpdater == null)
            {
                _crosshairUpdater = _rectCrosshair.gameObject.AddComponent<CrosshairUpdater>();
            }
            _crosshairUpdater.Initialize(this, _rectCrosshair, Canvas);
        }

        /// <summary>
        /// 循环切换到下一个准星样式。
        /// </summary>
        public void CycleCrosshairStyle()
        {
            var values = (CrosshairStyle[])System.Enum.GetValues(typeof(CrosshairStyle));
            int idx = System.Array.IndexOf(values, _currentStyle);
            int next = (idx + 1) % values.Length;
            SetCrosshairStyle(values[next]);
            Log.Debug($"[BattleMainUI] 切换准星样式: {_currentStyle}");
        }

        /// <summary>
        /// 设置当前准星样式。
        /// </summary>
        public void SetCrosshairStyle(CrosshairStyle style)
        {
            _currentStyle = style;
            if (_crosshairImage != null && _crosshairSprites.TryGetValue(style, out var sprite))
            {
                _crosshairImage.sprite = sprite;
                _crosshairImage.color = _crosshairColor;
            }
        }

        /// <summary>
        /// 预生成所有准星样式的 Sprite。
        /// </summary>
        private void GenerateAllCrosshairSprites()
        {
            _crosshairSprites[CrosshairStyle.Dot] = CreateDotSprite();
            _crosshairSprites[CrosshairStyle.Cross] = CreateCrossSprite();
            _crosshairSprites[CrosshairStyle.Circle] = CreateCircleSprite();
            _crosshairSprites[CrosshairStyle.TShape] = CreateTShapeSprite();
        }

        private Sprite CreateDotSprite()
        {
            int size = _crosshairSize;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            ClearTexture(tex);
            float radius = size * 0.25f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            FillCircle(tex, center, radius, _crosshairColor);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite CreateCrossSprite()
        {
            int size = _crosshairSize;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            ClearTexture(tex);
            float half = size * 0.5f;
            float halfThick = _crosshairThickness * 0.5f;

            // 水平线
            FillRect(tex, new Vector2(halfThick, half - halfThick), new Vector2(size - halfThick, half + halfThick), _crosshairColor);
            // 垂直线
            FillRect(tex, new Vector2(half - halfThick, halfThick), new Vector2(half + halfThick, size - halfThick), _crosshairColor);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite CreateCircleSprite()
        {
            int size = _crosshairSize;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            ClearTexture(tex);
            float radius = size * 0.5f - 2f;
            float thickness = _crosshairThickness;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            DrawRing(tex, center, radius, thickness, _crosshairColor);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite CreateTShapeSprite()
        {
            int size = _crosshairSize;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            ClearTexture(tex);
            float half = size * 0.5f;
            float halfThick = _crosshairThickness * 0.5f;

            // 顶部横线
            FillRect(tex, new Vector2(halfThick, size - _crosshairThickness - 1f), new Vector2(size - halfThick, size - 1f), _crosshairColor);
            // 中间竖线
            FillRect(tex, new Vector2(half - halfThick, halfThick), new Vector2(half + halfThick, size - _crosshairThickness - 1f), _crosshairColor);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        #region 纹理绘制辅助

        private void ClearTexture(Texture2D tex)
        {
            int size = tex.width;
            var clear = new Color[size * size];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
        }

        private void FillRect(Texture2D tex, Vector2 min, Vector2 max, Color color)
        {
            int xMin = Mathf.Max(0, Mathf.FloorToInt(min.x));
            int yMin = Mathf.Max(0, Mathf.FloorToInt(min.y));
            int xMax = Mathf.Min(tex.width - 1, Mathf.CeilToInt(max.x));
            int yMax = Mathf.Min(tex.height - 1, Mathf.CeilToInt(max.y));

            for (int x = xMin; x <= xMax; x++)
            for (int y = yMin; y <= yMax; y++)
                tex.SetPixel(x, y, color);
        }

        private void FillCircle(Texture2D tex, Vector2 center, float radius, Color color)
        {
            int r = Mathf.CeilToInt(radius);
            int cx = Mathf.RoundToInt(center.x);
            int cy = Mathf.RoundToInt(center.y);
            float r2 = radius * radius;

            for (int x = -r; x <= r; x++)
            for (int y = -r; y <= r; y++)
            {
                if (x * x + y * y <= r2)
                {
                    int px = cx + x;
                    int py = cy + y;
                    if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                        tex.SetPixel(px, py, color);
                }
            }
        }

        private void DrawRing(Texture2D tex, Vector2 center, float radius, float thickness, Color color)
        {
            int size = tex.width;
            float inner = Mathf.Max(0f, radius - thickness * 0.5f);
            float outer = radius + thickness * 0.5f;
            float inner2 = inner * inner;
            float outer2 = outer * outer;

            for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dx = x + 0.5f - center.x;
                float dy = y + 0.5f - center.y;
                float d2 = dx * dx + dy * dy;
                if (d2 >= inner2 && d2 <= outer2)
                    tex.SetPixel(x, y, color);
            }
        }

        #endregion

        #endregion

        private void OnHpChanged(int currentHp, int maxHp)
        {
            if (_textHp != null) _textHp.text = $"HP: {currentHp}/{maxHp}";
        }

        private void OnAmmoChanged(int currentAmmo, int maxAmmo)
        {
            if (_textAmmo != null) _textAmmo.text = $"Ammo: {currentAmmo}/{maxAmmo}";
        }

        private void OnWeaponEquipped(int ownerId, int slot, int weaponConfigId)
        {
            RefreshAll();
        }

        private void OnWeaponSwitched(int ownerId, int slot)
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            var playerSystem = PlayerSystem.Instance;
            if (playerSystem != null && _textHp != null)
            {
                _textHp.text = $"HP: {playerSystem.CurrentHp}/{playerSystem.MaxHp}";
            }
            else
            {
                if (_textHp != null) _textHp.text = "HP: -/-";
            }

            var weapon = WeaponSystem.Instance?.CurrentWeapon;
            if (weapon != null)
            {
                if (_textAmmo != null) _textAmmo.text = $"Ammo: {weapon.CurrentAmmo}/{weapon.Config.clipSize}";
                if (_textWeapon != null) _textWeapon.text = $"Weapon: {weapon.Config.name}";
            }
            else
            {
                if (_textAmmo != null) _textAmmo.text = "Ammo: -/-";
                if (_textWeapon != null) _textWeapon.text = "Weapon: -";
            }

            Log.Debug($"[BattleMainUI] RefreshAll: player={(playerSystem != null ? $"{playerSystem.CurrentHp}/{playerSystem.MaxHp}" : "null")}, weapon={(weapon != null ? weapon.Config.name : "null")}");
        }
    }
}
