namespace GameLogic.Portal
{
    /// <summary>
    /// 所有敌人被击败后激活传送门。
    /// </summary>
    public class AllEnemiesDefeatedCondition : IPortalCondition
    {
        public bool Evaluate(PortalEntity portal)
        {
            var system = PortalSystem.Instance;
            if (system == null) return false;
            // 避免开局敌人尚未生成时被误判为已全灭
            return system.TotalSpawnedEnemyCount > 0 && system.AliveEnemyCount <= 0;
        }
    }
}
