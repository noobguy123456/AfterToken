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
