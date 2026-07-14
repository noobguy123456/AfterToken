using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 狙击镜 UI。
    /// 黑屏遮罩，只在中心留一个圆形视野，显示 ScopeCamera 的 RenderTexture。
    /// </summary>
    [Window(UILayer.Top, location: "SniperScopeUI", fullScreen: true)]
    public class SniperScopeUI : UIWindow
    {
        #region 脚本工具生成的代码
        private Image _vignetteImage;
        private RawImage _scopeImage;

        protected override void ScriptGenerator()
        {
            _vignetteImage = FindChildComponent<Image>("m_img_Vignette");
            _scopeImage = FindChildComponent<RawImage>("m_raw_Scope");
        }
        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            ApplyVignetteSprite();
            RefreshScopeTexture();
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

        private void ApplyVignetteSprite()
        {
            if (_vignetteImage != null)
            {
                _vignetteImage.sprite = CreateCircleMaskSprite();
                _vignetteImage.type = Image.Type.Simple;
            }
        }

        private void RefreshScopeTexture()
        {
            if (_scopeImage != null && CameraSystem.Instance != null)
            {
                _scopeImage.texture = CameraSystem.Instance.ScopeRenderTexture;
            }
        }

        private Sprite CreateCircleMaskSprite()
        {
            // 中心透明、四周黑色的遮罩纹理
            int size = 512;
            var tex = new Texture2D(size, size);
            int radius = size / 4;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    Color c = dist < radius ? Color.clear : Color.black;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
