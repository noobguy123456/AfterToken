using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 敌人实体注册表。
    /// 由 <see cref="EnemyEntity"/> 在 OnEnable/OnDisable 时自动注册/注销，
    /// 供战斗系统、辅助瞄准、子弹追踪等查询，避免运行时 FindObjectsByType。
    /// </summary>
    public static class EnemyRegistry
    {
        private static readonly Dictionary<int, EnemyEntity> _byInstanceId = new();

        /// <summary>
        /// 当前所有已注册的敌人实体（只读）。
        /// </summary>
        public static IReadOnlyCollection<EnemyEntity> All => _byInstanceId.Values;

        /// <summary>
        /// 注册敌人实体。
        /// </summary>
        public static void Register(EnemyEntity entity)
        {
            if (entity == null) return;
            _byInstanceId[entity.GetInstanceID()] = entity;
        }

        /// <summary>
        /// 注销敌人实体。
        /// </summary>
        public static void Unregister(EnemyEntity entity)
        {
            if (entity == null) return;
            _byInstanceId.Remove(entity.GetInstanceID());
        }

        /// <summary>
        /// 根据 InstanceID 查找敌人实体。
        /// </summary>
        public static bool TryGet(int instanceId, out EnemyEntity entity)
        {
            return _byInstanceId.TryGetValue(instanceId, out entity);
        }

        /// <summary>
        /// 清空注册表（场景切换/重启时调用）。
        /// </summary>
        public static void Clear()
        {
            _byInstanceId.Clear();
        }
    }
}
