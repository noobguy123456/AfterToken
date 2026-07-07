namespace GameLogic.Portal
{
    /// <summary>
    /// 无条件，传送门始终处于激活状态。
    /// </summary>
    public class NoneCondition : IPortalCondition
    {
        public bool Evaluate(PortalEntity portal) => true;
    }
}
