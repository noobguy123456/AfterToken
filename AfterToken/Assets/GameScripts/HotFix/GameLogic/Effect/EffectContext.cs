using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 特效播放参数（struct，热路径零分配）。
    /// 字段含义由各特效驱动（EffectDriver）解释。
    /// </summary>
    public struct EffectContext
    {
        /// <summary>
        /// 缩放参数。爆炸类特效（scaleByRadius=true）按爆炸半径解释。
        /// </summary>
        public float Scale;

        public static EffectContext Default => new EffectContext { Scale = 1f };

        public EffectContext(float scale)
        {
            Scale = scale;
        }
    }
}
