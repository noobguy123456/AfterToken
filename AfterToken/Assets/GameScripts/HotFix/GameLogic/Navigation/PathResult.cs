using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Navigation
{
    /// <summary>
    /// 寻路结果。
    /// </summary>
    public class PathResult
    {
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

        public static PathResult Failed => new() { Success = false };
    }
}
