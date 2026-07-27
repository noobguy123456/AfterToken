using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 战斗场景边界。
    /// 由战斗流程在场景加载后用 Ground 的 Renderer.bounds 初始化，
    /// 玩家移动在 FixedUpdate 中通过 <see cref="Clamp"/> 钳制在地面范围内。
    /// </summary>
    public static class BattleBoundary
    {
        /// <summary>
        /// 钳制边距（米），防止角色中心贴到地面边缘时视觉越界。
        /// </summary>
        private const float CLAMP_MARGIN = 0.5f;

        private static Bounds _bounds;
        private static bool _hasBounds;

        /// <summary>
        /// 是否已初始化边界。
        /// </summary>
        public static bool HasBounds => _hasBounds;

        /// <summary>
        /// 初始化战斗边界。
        /// </summary>
        public static void Init(Bounds bounds)
        {
            _bounds = bounds;
            _hasBounds = true;
        }

        /// <summary>
        /// 清除边界（离开战斗流程时调用）。
        /// </summary>
        public static void Clear()
        {
            _hasBounds = false;
            _bounds = default;
        }

        /// <summary>
        /// 将世界坐标钳制在边界内（仅钳制 x/z，y 保持不变）。
        /// </summary>
        public static Vector3 Clamp(Vector3 position)
        {
            if (!_hasBounds)
            {
                return position;
            }

            position.x = Mathf.Clamp(position.x, _bounds.min.x + CLAMP_MARGIN, _bounds.max.x - CLAMP_MARGIN);
            position.z = Mathf.Clamp(position.z, _bounds.min.z + CLAMP_MARGIN, _bounds.max.z - CLAMP_MARGIN);
            return position;
        }
    }
}
