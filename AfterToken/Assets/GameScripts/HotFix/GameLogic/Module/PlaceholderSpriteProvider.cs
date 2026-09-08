using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 占位 Sprite 提供者。
    /// 统一缓存运行时动态创建的白色 Sprite，避免多个系统重复创建 Texture2D。
    /// </summary>
    public static class PlaceholderSpriteProvider
    {
        private static Sprite _whiteSprite16;
        private static Sprite _whiteSprite4;
        private static Sprite _ringSprite64;

        /// <summary>
        /// 获取 16x16 白色 Sprite（用于占位敌人、飞行物等）。
        /// </summary>
        public static Sprite GetWhiteSprite16()
        {
            if (_whiteSprite16 == null)
            {
                _whiteSprite16 = CreateWhiteSprite(16, new Vector2(0.5f, 0.5f), 16f);
            }
            return _whiteSprite16;
        }

        /// <summary>
        /// 获取 4x4 白色 Sprite（用于血条等）。
        /// </summary>
        public static Sprite GetWhiteSprite4()
        {
            if (_whiteSprite4 == null)
            {
                _whiteSprite4 = CreateWhiteSprite(4, new Vector2(0f, 0.5f), 4f);
            }
            return _whiteSprite4;
        }

        /// <summary>
        /// 获取 64x64 软边圆环 Sprite（用于可交互物的地面光圈等）。
        /// 环带位于半径 0.30~0.46（归一化），内外缘平滑过渡；中心枢轴，1 单位世界尺寸。
        /// </summary>
        public static Sprite GetRingSprite64()
        {
            if (_ringSprite64 == null)
            {
                _ringSprite64 = CreateRingSprite(64, 0.30f, 0.46f);
            }
            return _ringSprite64;
        }

        private static Sprite CreateRingSprite(int size, float innerRadius, float outerRadius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            float half = size * 0.5f;
            // 软边宽度（归一化半径）
            const float softness = 0.06f;
            for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float r = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center)) / half;
                // 环带中心最亮，向内外缘平滑衰减
                float bandCenter = (innerRadius + outerRadius) * 0.5f;
                float alpha = 1f - Mathf.Clamp01(Mathf.Abs(r - bandCenter) / ((outerRadius - innerRadius) * 0.5f + softness));
                alpha = alpha * alpha; // 平方让衰减更柔
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateWhiteSprite(int size, Vector2 pivot, float pixelsPerUnit)
        {
            var tex = new Texture2D(size, size);
            for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tex.SetPixel(x, y, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), pivot, pixelsPerUnit);
        }
    }
}
