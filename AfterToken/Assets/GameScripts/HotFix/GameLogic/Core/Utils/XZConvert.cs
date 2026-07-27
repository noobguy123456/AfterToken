using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// XZ 玩法平面坐标转换工具。
    /// 约定：玩法逻辑层的 Vector2 坐标语义为世界 (x, z)（俯视角 3D 地面），
    /// 与世界 Vector3 互转时通过本工具显式进行，禁止隐式强转（会取到 x, y）。
    /// </summary>
    public static class XZConvert
    {
        /// <summary>
        /// 世界坐标 → 玩法平面坐标 (x, z)。
        /// </summary>
        public static Vector2 ToXZ(this Vector3 v) => new Vector2(v.x, v.z);

        /// <summary>
        /// 玩法平面坐标 (x, z) → 世界坐标，y 默认为地面高度 0。
        /// </summary>
        public static Vector3 ToWorld(this Vector2 v, float y = 0f) => new Vector3(v.x, y, v.y);
    }
}
