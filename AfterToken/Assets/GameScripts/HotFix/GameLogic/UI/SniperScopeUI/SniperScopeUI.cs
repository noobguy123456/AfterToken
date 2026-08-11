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
        // 灰色蒙版不透明度（0~1）。全屏均匀压灰但不遮挡场景信息（Duckov 式）
        private const float VIGNETTE_ALPHA = 0.3f;
        // 灰色蒙版颜色（中性灰）
        private static readonly Color VIGNETTE_COLOR = new Color(0.5f, 0.5f, 0.5f, VIGNETTE_ALPHA);

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            ApplySprites();
            DisableRaycastTargets();
            EnsureDamageRoot();
            RefreshScopeTexture();
            _instance = this;
        }

        protected override void OnDestroy()
        {
            _instance = null;
            base.OnDestroy();
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
            // 镜窗（圆环+十字线）跟随准星（灵敏度驱动），保证镜窗中心 = 子弹落点；
            // 准星不可用时（非战斗场景）退回原始鼠标位置
            Vector3 mousePos = CrosshairUpdater.Instance != null
                ? (Vector3)CrosshairUpdater.Instance.CurrentScreenPos
                : Input.mousePosition;
            if (_scopeRect != null)
            {
                _scopeRect.position = mousePos;
            }

            // 开镜期间 DamageNumberUI 被框架隐藏（OnUpdate 停走），代驱动镜内飘字动画
            DamageNumberUI.TickExternal(Time.deltaTime);
        }

        /// <summary>
        /// 运行时生成/设置占位图形：全屏灰色蒙版、圆形遮罩、镜框圆环。
        /// </summary>
        private void ApplySprites()
        {
            if (_vignetteImage != null)
            {
                // 均匀灰色蒙版：铺满全屏、不随鼠标移动，不影响观察场景信息
                _vignetteImage.sprite = null;
                _vignetteImage.color = VIGNETTE_COLOR;
                var rt = _vignetteImage.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
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
