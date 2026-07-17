using System.Collections.Generic;
using GameLogic.Portal;

namespace GameLogic
{
    /// <summary>
    /// 传送门实体注册表。
    /// 由 <see cref="PortalEntity"/> 在 OnEnable/OnDisable 时自动注册/注销，
    /// 供 <see cref="PortalSystem"/> 查询，避免运行时 FindObjectsByType。
    /// </summary>
    public static class PortalRegistry
    {
        private static readonly List<PortalEntity> _portals = new();

        /// <summary>
        /// 当前所有已注册的传送门实体（只读）。
        /// </summary>
        public static IReadOnlyList<PortalEntity> All => _portals;

        /// <summary>
        /// 注册传送门实体。
        /// </summary>
        public static void Register(PortalEntity portal)
        {
            if (portal == null || _portals.Contains(portal)) return;
            _portals.Add(portal);
        }

        /// <summary>
        /// 注销传送门实体。
        /// </summary>
        public static void Unregister(PortalEntity portal)
        {
            if (portal == null) return;
            _portals.Remove(portal);
        }

        /// <summary>
        /// 清空注册表（场景切换/重启时调用）。
        /// </summary>
        public static void Clear()
        {
            _portals.Clear();
        }
    }
}
