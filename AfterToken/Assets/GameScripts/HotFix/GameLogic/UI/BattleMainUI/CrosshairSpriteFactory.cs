using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 准星样式精灵工厂。
    /// 各样式在指定尺寸上程序化绘制白色贴图（染色由使用方的 Image.color 完成），
    /// BattleMainUI 的战斗准星与 SettingsUI 的样式预览共用。
    /// </summary>
    public static class CrosshairSpriteFactory
    {
        /// <summary>
        /// 生成指定样式的准星精灵（白色）。调用方负责销毁（sprite 及其 texture）。
        /// </summary>
        public static Sprite Create(CrosshairStyle style, int size, float thickness)
        {
            switch (style)
            {
                case CrosshairStyle.Dot: return CreateDotSprite(size);
                case CrosshairStyle.Cross: return CreateCrossSprite(size, thickness);
                case CrosshairStyle.Circle: return CreateCircleSprite(size, thickness);
                case CrosshairStyle.TShape: return CreateTShapeSprite(size, thickness);
                case CrosshairStyle.Reloading: return CreateReloadingSprite(size, thickness);
                default: return CreateCrossSprite(size, thickness);
            }
        }

        private static Sprite CreateDotSprite(int size)
        {
            var tex = CreateTexture(size);
            ClearTexture(tex);
            float radius = size * 0.25f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            FillCircle(tex, center, radius, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateCrossSprite(int size, float thickness)
        {
            var tex = CreateTexture(size);
            ClearTexture(tex);
            float half = size * 0.5f;
            float halfThick = thickness * 0.5f;

            // 水平线
            FillRect(tex, new Vector2(halfThick, half - halfThick), new Vector2(size - halfThick, half + halfThick), Color.white);
            // 垂直线
            FillRect(tex, new Vector2(half - halfThick, halfThick), new Vector2(half + halfThick, size - halfThick), Color.white);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateCircleSprite(int size, float thickness)
        {
            var tex = CreateTexture(size);
            ClearTexture(tex);
            float radius = size * 0.5f - 2f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            DrawRing(tex, center, radius, thickness, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateTShapeSprite(int size, float thickness)
        {
            var tex = CreateTexture(size);
            ClearTexture(tex);
            float half = size * 0.5f;
            float halfThick = thickness * 0.5f;

            // 顶部横线
            FillRect(tex, new Vector2(halfThick, size - thickness - 1f), new Vector2(size - halfThick, size - 1f), Color.white);
            // 中间竖线
            FillRect(tex, new Vector2(half - halfThick, halfThick), new Vector2(half + halfThick, size - thickness - 1f), Color.white);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>
        /// 换弹转圈准星：一个缺口的圆环，旋转时产生等待/加载视觉效果。
        /// </summary>
        private static Sprite CreateReloadingSprite(int size, float thickness)
        {
            var tex = CreateTexture(size);
            ClearTexture(tex);
            float radius = size * 0.5f - 2f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            // 绘制约 270 度的圆环，留一个缺口
            DrawArcRing(tex, center, radius, thickness, Color.white, 45f, 315f);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        #region 纹理绘制辅助

        private static Texture2D CreateTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        private static void ClearTexture(Texture2D tex)
        {
            int size = tex.width;
            var clear = new Color[size * size];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
        }

        private static void FillRect(Texture2D tex, Vector2 min, Vector2 max, Color color)
        {
            int xMin = Mathf.Max(0, Mathf.FloorToInt(min.x));
            int yMin = Mathf.Max(0, Mathf.FloorToInt(min.y));
            int xMax = Mathf.Min(tex.width - 1, Mathf.CeilToInt(max.x));
            int yMax = Mathf.Min(tex.height - 1, Mathf.CeilToInt(max.y));

            for (int x = xMin; x <= xMax; x++)
            for (int y = yMin; y <= yMax; y++)
                tex.SetPixel(x, y, color);
        }

        private static void FillCircle(Texture2D tex, Vector2 center, float radius, Color color)
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

        private static void DrawRing(Texture2D tex, Vector2 center, float radius, float thickness, Color color)
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

        /// <summary>
        /// 绘制指定角度范围的圆环。
        /// </summary>
        private static void DrawArcRing(Texture2D tex, Vector2 center, float radius, float thickness, Color color, float startAngle, float endAngle)
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
                if (d2 < inner2 || d2 > outer2) continue;

                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                angle = (angle + 360f) % 360f;

                float start = (startAngle + 360f) % 360f;
                float end = (endAngle + 360f) % 360f;

                bool inArc = start <= end
                    ? angle >= start && angle <= end
                    : angle >= start || angle <= end;

                if (inArc)
                    tex.SetPixel(x, y, color);
            }
        }

        #endregion
    }
}
