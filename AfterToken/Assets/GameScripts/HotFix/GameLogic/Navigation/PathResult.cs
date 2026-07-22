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
        private static readonly Stack<PathResult> _pool = new Stack<PathResult>();

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
        /// 从对象池获取一个 PathResult（已清空旧数据）。
        /// </summary>
        public static PathResult Acquire()
        {
            if (_pool.Count > 0)
            {
                var result = _pool.Pop();
                result.Success = false;
                result.Waypoints.Clear();
                result.PathLength = 0f;
                return result;
            }
            return new PathResult();
        }

        /// <summary>
        /// 将 PathResult 归还对象池。
        /// </summary>
        public static void Release(PathResult result)
        {
            if (result == null || result == _failed) return;
            result.Success = false;
            result.Waypoints.Clear();
            result.PathLength = 0f;
            _pool.Push(result);
        }

        /// <summary>
        /// 失败的寻路结果。返回的是共享实例，调用者不应修改其 Waypoints。
        /// </summary>
        public static PathResult Failed => _failed;
    }
}
