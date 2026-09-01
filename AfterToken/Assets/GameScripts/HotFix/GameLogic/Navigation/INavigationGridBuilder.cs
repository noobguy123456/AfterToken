namespace GameLogic.Navigation
{
    /// <summary>
    /// 导航网格生成器接口。
    /// </summary>
    public interface INavigationGridBuilder
    {
        /// <summary>
        /// 构建并返回导航网格。
        /// </summary>
        NavigationGrid Build();

        /// <summary>
        /// 局部更新网格：对 worldBounds 覆盖的格子重新判定可走性。
        /// </summary>
        void UpdateRegion(NavigationGrid grid, UnityEngine.Bounds worldBounds);
    }
}
