using UnityEngine;

namespace GameLogic.Navigation
{
    /// <summary>
    /// 导航系统接口。
    /// 所有移动逻辑只应依赖此接口，不依赖具体算法实现。
    /// </summary>
    public interface INavigationSystem
    {
        /// <summary>
        /// 设置导航网格。
        /// </summary>
        void SetGrid(NavigationGrid grid);

        /// <summary>
        /// 从世界坐标 from 到 to 寻找路径。
        /// </summary>
        PathResult FindPath(Vector2 from, Vector2 to);

        /// <summary>
        /// 判断世界坐标是否可行走。
        /// </summary>
        bool IsWalkable(Vector2 worldPos);

        /// <summary>
        /// 重新构建网格。
        /// </summary>
        void Rebuild();
    }
}
