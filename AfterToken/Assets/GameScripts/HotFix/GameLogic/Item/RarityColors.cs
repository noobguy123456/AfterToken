using GameConfig.cfg;
using UnityEngine;
using Color = UnityEngine.Color;

namespace GameLogic
{
    /// <summary>
    /// 稀有度颜色映射（占位期）。
    /// 世界掉落物染色与 UI 稀有度框共用；接入美术资源后可替换为框图/贴图。
    /// </summary>
    public static class RarityColors
    {
        /// <summary>
        /// 获取空槽位默认颜色（深灰，无稀有度）。
        /// </summary>
        public static Color GetDefault()
        {
            return new Color(0.2f, 0.2f, 0.2f, 1f);
        }

        /// <summary>
        /// 获取稀有度对应颜色：蓝 / 紫 / 黄 / 红。
        /// </summary>
        public static Color Get(EQuality quality)
        {
            switch (quality)
            {
                case EQuality.Blue: return new Color(0.3f, 0.55f, 1f);
                case EQuality.Purple: return new Color(0.7f, 0.35f, 0.95f);
                case EQuality.Yellow: return new Color(1f, 0.85f, 0.2f);
                case EQuality.Red: return new Color(1f, 0.3f, 0.25f);
                default: return Color.white;
            }
        }
    }
}
