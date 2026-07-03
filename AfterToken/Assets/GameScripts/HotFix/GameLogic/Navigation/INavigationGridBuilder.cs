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
    }
}
