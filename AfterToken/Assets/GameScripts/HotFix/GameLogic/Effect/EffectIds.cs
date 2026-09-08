namespace GameLogic
{
    /// <summary>
    /// 特效资源地址常量（YooAsset 按文件名寻址，与收集器 AddressByFileName 一致）。
    /// 新增特效（枪口火焰、命中火花等）在此加常量，prefab 放 Assets/AssetRaw/Effects/。
    /// </summary>
    public static class EffectIds
    {
        /// <summary>
        /// 火箭筒爆炸特效。
        /// </summary>
        public const string Explosion = "Explosion";

        /// <summary>
        /// 枪口火焰（开火瞬间定向闪光，World 挂载不跟随）。
        /// </summary>
        public const string MuzzleFlash = "MuzzleFlash";

        /// <summary>
        /// 命中火花（命中敌人，橙红）。
        /// </summary>
        public const string HitSpark = "HitSpark";

        /// <summary>
        /// 命中碎屑（命中场景/障碍，灰白）。与 HitSpark 同 shader，材质 _Tint 区分。
        /// </summary>
        public const string HitSparkEnv = "HitSparkEnv";

        /// <summary>
        /// 拾取光晕（掉落物常驻呼吸光圈，Follow 挂载，拾取/销毁时回收）。
        /// </summary>
        public const string PickupGlow = "PickupGlow";
    }
}
