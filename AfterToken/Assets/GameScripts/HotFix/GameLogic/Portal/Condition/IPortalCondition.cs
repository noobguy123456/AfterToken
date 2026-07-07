namespace GameLogic.Portal
{
    /// <summary>
    /// 传送门出现条件评估接口。
    /// </summary>
    public interface IPortalCondition
    {
        /// <summary>
        /// 评估条件是否满足。
        /// </summary>
        /// <param name="portal">要评估的传送门实体。</param>
        /// <returns>条件是否满足。</returns>
        bool Evaluate(PortalEntity portal);
    }
}
