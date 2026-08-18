using System.Collections.Generic;
using GameLogic.Loot;

namespace GameLogic
{
    /// <summary>
    /// 战利品容器实体注册表。
    /// 由 <see cref="LootContainerEntity"/> 在 OnEnable/OnDisable 时自动注册/注销，
    /// 供 <see cref="LootContainerSystem"/> 查询，避免运行时 FindObjectsByType。
    /// </summary>
    public static class LootContainerRegistry
    {
        private static readonly List<LootContainerEntity> _containers = new();

        /// <summary>
        /// 当前所有已注册的容器实体（只读）。
        /// </summary>
        public static IReadOnlyList<LootContainerEntity> All => _containers;

        /// <summary>
        /// 注册容器实体。
        /// </summary>
        public static void Register(LootContainerEntity container)
        {
            if (container == null || _containers.Contains(container)) return;
            _containers.Add(container);
        }

        /// <summary>
        /// 注销容器实体。
        /// </summary>
        public static void Unregister(LootContainerEntity container)
        {
            if (container == null) return;
            _containers.Remove(container);
        }

        /// <summary>
        /// 清空注册表（场景切换/重启时调用）。
        /// </summary>
        public static void Clear()
        {
            _containers.Clear();
        }
    }
}
