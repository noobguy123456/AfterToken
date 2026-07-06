using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Navigation
{
    /// <summary>
    /// 寻路结果。
    /// </summary>
    public class PathResult
    {
        private static readonly PathResult _failed = new() { Success = false };

        /// <summary>
        /// 是否成功找到路径。
        /// </summary>
        public bool Success;

        /// <summary>
        /// 路径点列表（世界坐标）。
        /// </summary>
        public List<Vector2> Waypoints;

        /// <summary>
        /// 路径总长度。
        /// </summary>
        public float PathLength;

        public PathResult()
        {
            Waypoints = new List<Vector2>();
        }

        /// <summary>
        /// 失败的寻路结果。返回的是共享实例，调用者不应修改其 Waypoints。
        /// </summary>
        public static PathResult Failed => _failed;
    }
}
