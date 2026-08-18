using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 狙击镜 UI（Escape from Duckov 式）。
    /// 一个跟随鼠标的圆形镜窗：窗内显示 ScopeCamera 的放大画面（RenderTexture），
    /// 窗外用带圆孔的暗角遮罩压暗（半透明，隐约可见）。
    /// 全部图形关闭 raycastTarget，不拦截战斗输入。
    /// </summary>
    [Window(UILayer.Top, location: "SniperScopeUI", fullScreen: true)]
    public class SniperScopeUI : UIWindow
    {
        #region 脚本工具生成的代码
        private Image _vignetteImage;
        private RectTransform _scopeRect;
        private Image _maskImage;
        private RawImage _scopeImage;
        private Image _ringImage;
        private Image _crossHImage;
        private Image _crossVImage;

        // 镜内伤害数字容器（运行时创建，挂在镜窗下，随镜窗一起跟随鼠标）
        private RectTransform _damageRoot;
        private static SniperScopeUI _instance;

        protected override void ScriptGenerator()
        {
            _vignetteImage = FindChildComponent<Image>("m_img_Vignette");
            _scopeRect = FindChild("m_rect_Scope") as RectTransform;
            _maskImage = FindChildComponent<Image>("m_rect_Scope/m_mask_Scope");
            _scopeImage = FindChildComponent<RawImage>("m_rect_Scope/m_mask_Scope/m_raw_Scope");
            _ringImage = FindChildComponent<Image>("m_rect_Scope/m_img_Ring");
            _crossHImage = FindChildComponent<Image>("m_rect_Scope/m_img_CrossH");
            _crossVImage = FindChildComponent<Image>("m_rect_Scope/m_img_CrossV");
        }
        #endregion

        // 镜窗直径（UI 逻辑像素）。根 Canvas scaleFactor≈2.56，1080p 下实际显示 ≈640px，
        // 约 0.59 屏高，对齐 Duckov 狙击镜比例（约 0.55~0.6 屏高）
        private const float SCOPE_DIAMETER = 250f;
        // 灰色蒙版不透明度（0~1）。全屏压灰但不遮挡场景信息（Duckov 式），镜窗圆孔内完全透明
        private const float VIGNETTE_ALPHA = 0.3f;
        // 灰色蒙版颜色（中性灰）
        private static readonly Color VIGNETTE_COLOR = new Color(0.5f, 0.5f, 0.5f, VIGNETTE_ALPHA);
        // 灰色蒙版边长：足够大，保证圆孔跟随准星到屏幕任意角落后蒙版仍能盖住全屏
        private const float VIGNETTE_SIZE = 5000f;

        // ---- 命中标记（hitmarker）----
        // 标记尺寸（逻辑像素）与显示时长（秒）
        private const float HitMarkerSize = 44f;
        private const float HitMarkerDuration = 0.25f;
        private static readonly Color HitMarkerNormalColor = new Color(1f, 1f, 1f, 1f);
        private static readonly Color HitMarkerCriticalColor = new Color(1f, 0.55f, 0.1f, 1f);
        private static readonly Color HitMarkerKillColor = new Color(1f, 0.15f, 0.1f, 1f);
        private Image _hitMarker;
        private Color _hitMarkerColor = HitMarkerNormalColor;
        private float _hitMarkerTimer;

        // ---- 后坐力镜窗跳动（屏幕像素偏移，指数回弹）----
        private const float KickUpPixels = 30f;   // 开火上跳幅度（屏幕像素）
        private const float KickSidePixels = 7f;  // 横向随机抖动幅度
        private const float KickRecoverRate = 12f;// 回弹速率（指数衰减）
        private Vector2 _kickOffset;

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            ApplySprites();
            DisableRaycastTargets();
            EnsureHitMarker();
            EnsureDamageRoot();
            RefreshScopeTexture();
            _instance = this;

            // 开火后坐力 + 命中/击杀标记（窗口仅开镜期间存在，事件天然只在开镜时响应）
            AddUIEvent<Vector2, Vector2, int, int>(IWeaponEvent_Event.OnFire, OnFireRecoil);
            AddUIEvent<bool, Vector2>(IHitFeedbackEvent_Event.OnHitTarget, OnScopeHitTarget);
            AddUIEvent<int, int>(IBattleEvent_Event.OnEntityKilled, OnScopeEntityKilled);
        }

        protected override void OnDestroy()
        {
            _instance = null;
            base.OnDestroy();
        }

        /// <summary>
        /// 运行时创建命中标记：挂在镜窗中心（hitmarker 出现在准星点，即子弹落点）。
        /// 开镜直接命中模型下命中点恒为镜窗中心，无需按世界坐标换算。
        /// </summary>
        private void EnsureHitMarker()
        {
            if (_hitMarker != null || _scopeRect == null) return;
            var go = new GameObject("m_img_ScopeHitMarker", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_scopeRect, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(HitMarkerSize, HitMarkerSize);
            _hitMarker = go.GetComponent<Image>();
            _hitMarker.sprite = CreateHitMarkerSprite();
            _hitMarker.raycastTarget = false;
            _hitMarker.enabled = false;
        }

        /// <summary>
        /// 开火后坐力：镜窗（连同蒙版圆孔）向上跳一下并带回横向抖动，指数回弹。
        /// </summary>
        private void OnFireRecoil(Vector2 origin, Vector2 direction, int weaponConfigId, int ownerId)
        {
            // 仅玩家自己开镜狙击时跳动（事件对全局所有武器开火都会发）
            if (WeaponSystem.Instance == null || !WeaponSystem.Instance.IsScopedSniping) return;
            _kickOffset += new Vector2(Random.Range(-KickSidePixels, KickSidePixels), KickUpPixels);
        }

        /// <summary>
        /// 命中反馈：镜窗中心显示白色 ×（暴击橙色）。
        /// </summary>
        private void OnScopeHitTarget(bool isCritical, Vector2 screenPos)
        {
            ShowScopeHitMarker(isCritical ? HitMarkerCriticalColor : HitMarkerNormalColor);
        }

        /// <summary>
        /// 击杀反馈：玩家击杀时显示红色 ×（覆盖普通命中色）。
        /// </summary>
        private void OnScopeEntityKilled(int attackerId, int targetId)
        {
            var player = PlayerSystem.Instance != null ? PlayerSystem.Instance.GetPlayerEntity() : null;
            if (player != null && attackerId == player.GetInstanceID())
            {
                ShowScopeHitMarker(HitMarkerKillColor);
            }
        }

        private void ShowScopeHitMarker(Color color)
        {
            if (_hitMarker == null) return;
            _hitMarkerColor = color;
            _hitMarker.enabled = true;
            _hitMarkerTimer = HitMarkerDuration;
        }

        /// <summary>
        /// 命中标记精灵：四根对角短刺（中心留空），白色，由 Image.color 染色。
        /// </summary>
        private Sprite CreateHitMarkerSprite()
        {
            const int size = 64;
            const float halfThick = 2.5f; // 刺半宽
            const float inner = 10f;      // 中心留空半径
            const float outer = 26f;      // 刺尖半径
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x - c, v = y - c;
                    // 到两条对角线的距离
                    float d1 = Mathf.Abs(u - v) * 0.7071f;
                    float d2 = Mathf.Abs(u + v) * 0.7071f;
                    float r = Mathf.Max(Mathf.Abs(u), Mathf.Abs(v)); // 切比雪夫半径
                    float alpha = 0f;
                    if (r > inner && r < outer)
                    {
                        float d = Mathf.Min(d1, d2);
                        alpha = Mathf.Clamp01(halfThick - d);
                    }
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// 运行时创建镜内伤害数字容器：铺满镜窗，随 _scopeRect 一起跟随鼠标。
        /// 飘字本体复用 DamageNumberUI 的对象池与动画（见 DamageNumberUI.ShowLocal）。
        /// </summary>
        private void EnsureDamageRoot()
        {
            if (_damageRoot != null || _scopeRect == null) return;
            var go = new GameObject("m_rect_ScopeDamage", typeof(RectTransform));
            _damageRoot = (RectTransform)go.transform;
            _damageRoot.SetParent(_scopeRect, false);
            _damageRoot.anchorMin = Vector2.zero;
            _damageRoot.anchorMax = Vector2.one;
            _damageRoot.offsetMin = Vector2.zero;
            _damageRoot.offsetMax = Vector2.zero;
            // 飘字显示在准星（十字线）之上
            _damageRoot.SetAsLastSibling();
        }

        /// <summary>
        /// 开镜命中时在镜窗内显示伤害数字。
        /// 按狙击镜相机的取景变换把世界坐标换算到镜窗局部坐标，复用 DamageNumberUI 飘字。
        /// </summary>
        /// <returns>是否成功在镜内生成；false 时调用方应走主相机屏幕坐标原路径。</returns>
        public static bool ShowScopeDamage(int damage, Vector3 worldPos, bool isCritical = false)
        {
            return _instance != null && _instance.ShowScopeDamageInternal(damage, worldPos, isCritical);
        }

        private bool ShowScopeDamageInternal(int damage, Vector3 worldPos, bool isCritical)
        {
            var camSys = CameraSystem3D.Instance;
            if (camSys == null || _damageRoot == null) return false;
            if (!camSys.TryWorldToScopeViewportPoint(worldPos, out var viewport)) return false;

            // 镜内视口坐标（0~1，中心 0.5）→ 镜窗局部坐标（与镜窗显示区同尺寸，中心为原点）
            Vector2 scopeSize = _scopeImage != null
                ? _scopeImage.rectTransform.rect.size
                : new Vector2(SCOPE_DIAMETER, SCOPE_DIAMETER);
            Vector2 localPos = (viewport - new Vector2(0.5f, 0.5f)) * scopeSize;
            DamageNumberUI.ShowLocal(_damageRoot, damage, localPos, isCritical);
            return true;
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            // 开镜瞬间清掉 HitFeedbackUI 里未淡出完的命中标记：
            // 狙击镜全屏遮挡该窗口后其 OnUpdate 停走，残留标记会冻结在画面上。
            HitFeedbackUI.Instance?.ClearHitMarkers();
            RefreshScopeTexture();
        }

        protected override void OnSetVisible(bool visible)
        {
            base.OnSetVisible(visible);
            if (visible)
            {
                RefreshScopeTexture();
            }
        }

        protected override void OnUpdate()
        {
            // 镜窗（圆环+十字线）与蒙版圆孔整体跟随准星（灵敏度驱动），保证镜窗中心 = 子弹落点；
            // 准星不可用时（非战斗场景）退回原始鼠标位置。后坐力跳动叠加在跟随位置上。
            Vector3 mousePos = CrosshairUpdater.Instance != null
                ? (Vector3)CrosshairUpdater.Instance.CurrentScreenPos
                : Input.mousePosition;
            _kickOffset = Vector2.Lerp(_kickOffset, Vector2.zero,
                1f - Mathf.Exp(-KickRecoverRate * Time.deltaTime));
            Vector3 finalPos = mousePos + (Vector3)_kickOffset;
            if (_scopeRect != null)
            {
                _scopeRect.position = finalPos;
            }
            if (_vignetteImage != null)
            {
                _vignetteImage.rectTransform.position = finalPos;
            }

            // 命中标记：缩放 punch + 淡出
            if (_hitMarker != null && _hitMarker.enabled)
            {
                _hitMarkerTimer -= Time.deltaTime;
                if (_hitMarkerTimer <= 0f)
                {
                    _hitMarker.enabled = false;
                }
                else
                {
                    float t = Mathf.Clamp01(_hitMarkerTimer / HitMarkerDuration);
                    _hitMarker.color = new Color(_hitMarkerColor.r, _hitMarkerColor.g, _hitMarkerColor.b, t);
                    float s = Mathf.Lerp(1f, 1.35f, t);
                    _hitMarker.rectTransform.localScale = new Vector3(s, s, 1f);
                }
            }

            // 开镜期间 DamageNumberUI 被框架隐藏（OnUpdate 停走），代驱动镜内飘字动画
            DamageNumberUI.TickExternal(Time.deltaTime);
        }

        /// <summary>
        /// 运行时生成/设置占位图形：带圆孔的灰色蒙版、圆形遮罩、镜框圆环。
        /// </summary>
        private void ApplySprites()
        {
            if (_vignetteImage != null)
            {
                // 带圆孔的灰色蒙版：圆孔与镜窗同径、跟随准星，孔内零遮挡正常渲染，孔外压灰
                _vignetteImage.sprite = CreateVignetteSprite();
                _vignetteImage.type = Image.Type.Simple;
                _vignetteImage.color = VIGNETTE_COLOR;
                _vignetteImage.rectTransform.sizeDelta = new Vector2(VIGNETTE_SIZE, VIGNETTE_SIZE);
            }

            if (_maskImage != null)
            {
                _maskImage.sprite = CreateCircleSprite(256, 0f);
                _maskImage.type = Image.Type.Simple;
            }

            if (_ringImage != null)
            {
                _ringImage.sprite = CreateRingSprite();
                _ringImage.type = Image.Type.Simple;
                _ringImage.rectTransform.sizeDelta = new Vector2(SCOPE_DIAMETER, SCOPE_DIAMETER);
            }

            // 镜窗整体与十字线长度对齐圆环直径（prefab 里是旧值 480，运行时统一为 SCOPE_DIAMETER）
            if (_scopeRect != null)
            {
                _scopeRect.sizeDelta = new Vector2(SCOPE_DIAMETER, SCOPE_DIAMETER);
            }
            if (_crossHImage != null)
            {
                _crossHImage.rectTransform.sizeDelta = new Vector2(SCOPE_DIAMETER, 2f);
            }
            if (_crossVImage != null)
            {
                _crossVImage.rectTransform.sizeDelta = new Vector2(2f, SCOPE_DIAMETER);
            }
        }

        private void DisableRaycastTargets()
        {
            // 狙击镜 UI 永不拦截点击（十字线在 prefab 中已关闭 raycastTarget）
            if (_vignetteImage != null) _vignetteImage.raycastTarget = false;
            if (_maskImage != null) _maskImage.raycastTarget = false;
            if (_scopeImage != null) _scopeImage.raycastTarget = false;
            if (_ringImage != null) _ringImage.raycastTarget = false;
        }

        private void RefreshScopeTexture()
        {
            if (_scopeImage != null)
            {
                _scopeImage.texture = CameraSystem3D.Instance != null
                    ? CameraSystem3D.Instance.ScopeRenderTexture
                    : null;
                // scopeFov=0（无放大）时没有镜相机/渲染纹理，隐藏放大画面，只留镜窗图案
                _scopeImage.enabled = _scopeImage.texture != null;
            }
        }

        /// <summary>
        /// 带圆孔的遮罩纹理：中心圆形透明孔（与镜窗同半径），四周纯白不透明。
        /// 像素色为白、由 Image.color 乘成灰色，孔内 alpha=0 完全透出场景。
        /// </summary>
        private Sprite CreateVignetteSprite()
        {
            const int texSize = 1024;
            float texScale = VIGNETTE_SIZE / texSize; // 纹理像素 -> 屏幕像素
            float holeRadius = (SCOPE_DIAMETER * 0.5f) / texScale;
            float feather = 6f; // 边缘过渡（纹理像素）

            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(texSize * 0.5f, texSize * 0.5f);
            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01((dist - holeRadius) / feather);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// 实心圆（Mask 用，边缘羽化 anti-aliasing）。
        /// </summary>
        private Sprite CreateCircleSprite(int texSize, float feather)
        {
            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            float radius = texSize * 0.5f - 1f;
            Vector2 center = new Vector2(texSize * 0.5f, texSize * 0.5f);
            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - dist + Mathf.Max(feather, 1f));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// 圆环（镜框描边）。
        /// </summary>
        private Sprite CreateRingSprite()
        {
            const int texSize = 256;
            const float ringWidth = 8f;
            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            float outer = texSize * 0.5f - 1f;
            float inner = outer - ringWidth;
            Vector2 center = new Vector2(texSize * 0.5f, texSize * 0.5f);
            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    bool inRing = dist <= outer && dist >= inner;
                    tex.SetPixel(x, y, inRing ? new Color(0.1f, 0.1f, 0.1f, 0.95f) : Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f));
        }
    }
}
