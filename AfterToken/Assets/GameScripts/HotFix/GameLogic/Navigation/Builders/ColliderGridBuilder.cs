using TEngine;
using UnityEngine;

namespace GameLogic.Navigation
{
    /// <summary>
    /// 基于 Collider 自动生成导航网格。
    /// </summary>
    public class ColliderGridBuilder : INavigationGridBuilder
    {
        /// <summary>
        /// TbEnemy 表不可用/为空时的导航代理半径兜底值（米）。
        /// </summary>
        internal const float FallbackAgentRadius = 0.3f;

        private static bool _fallbackWarned;

        /// <summary>
        /// 导航代理半径（米）：障碍按此半径膨胀，路径不再贴障碍边。
        /// 运行时取 TbEnemy 全表最大 radius；表不可用/为空时兜底 <see cref="FallbackAgentRadius"/>。
        /// </summary>
        internal static float AgentRadius
        {
            get
            {
                float maxRadius = 0f;
                bool found = false;
                try
                {
                    var tbEnemy = ConfigSystem.Instance.Tables?.TbEnemy;
                    if (tbEnemy != null)
                    {
                        foreach (var enemy in tbEnemy.DataList)
                        {
                            if (enemy.Radius > maxRadius)
                            {
                                maxRadius = enemy.Radius;
                            }
                            found = true;
                        }
                    }
                }
                catch (System.Exception)
                {
                    // 配置系统未就绪（如编辑器下直接触发），走兜底
                }

                if (!found || maxRadius <= 0f)
                {
                    if (!_fallbackWarned)
                    {
                        _fallbackWarned = true;
                        Log.Warning($"[ColliderGridBuilder] TbEnemy 表不可用或无有效 radius，导航代理半径兜底为 {FallbackAgentRadius}m");
                    }
                    return FallbackAgentRadius;
                }
                return maxRadius;
            }
        }

        private readonly float _cellSize;
        private readonly float _margin;
        private readonly LayerMask _obstacleMask;
        private readonly Vector2? _forcedBoundsCenter;
        private readonly Vector2? _forcedBoundsSize;
        private readonly Vector2? _scanCenter;
        private readonly float? _scanRadius;

        public ColliderGridBuilder(
            float cellSize = 0.5f,
            float margin = 2f,
            LayerMask? obstacleMask = null,
            Vector2? forcedBoundsCenter = null,
            Vector2? forcedBoundsSize = null,
            Vector2? scanCenter = null,
            float? scanRadius = null)
        {
            _cellSize = cellSize;
            _margin = margin;
            _obstacleMask = obstacleMask ?? LayerMask.GetMask("Obstacle");
            _forcedBoundsCenter = forcedBoundsCenter;
            _forcedBoundsSize = forcedBoundsSize;
            _scanCenter = scanCenter;
            _scanRadius = scanRadius;
        }

        public NavigationGrid Build()
        {
            Bounds bounds = CalculateBounds();
            // 玩法平面为世界 (x, z)，网格边界取 bounds 的 x/z 分量
            Vector2 min = new Vector2(bounds.min.x, bounds.min.z);
            Vector2 max = new Vector2(bounds.max.x, bounds.max.z);

            int width = Mathf.CeilToInt((max.x - min.x) / _cellSize);
            int height = Mathf.CeilToInt((max.y - min.y) / _cellSize);

            if (width <= 0 || height <= 0)
            {
                Log.Warning("[ColliderGridBuilder] 计算出的网格尺寸无效，使用默认 10x10 网格");
                width = 10;
                height = 10;
            }

            var grid = new NavigationGrid
            {
                Origin = min,
                CellSize = _cellSize,
                Width = width,
                Height = height,
                Walkable = new bool[width * height]
            };

            // 检测球半径取导航代理半径，障碍按敌人半径膨胀，路径不贴障碍边；
            // 忽略 trigger，避免交互触发区（纸条/箱子 2m 触发盒）被误判为障碍
            float checkRadius = AgentRadius;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 center = grid.GetWorldPosition(x, y);
                    // 检测高度取 0.5f，与地面上的障碍物碰撞体对齐
                    bool blocked = Physics.CheckSphere(center.ToWorld(0.5f), checkRadius, _obstacleMask, QueryTriggerInteraction.Ignore);
                    grid.Walkable[grid.GetIndex(x, y)] = !blocked;
                }
            }

            Log.Info($"[ColliderGridBuilder] 网格构建完成：{width}x{height}，agentRadius={checkRadius}m（TbEnemy 最大 radius）");
            return grid;
        }

        /// <summary>
        /// 局部更新网格：对 worldBounds 覆盖的格子重新判定可走性（复用与烘焙一致的 CheckSphere 逻辑）。
        /// 越界格子 clamp 到网格范围内；网格为空时静默忽略。
        /// </summary>
        public void UpdateRegion(NavigationGrid grid, Bounds worldBounds)
        {
            if (grid == null) return;

            Vector2 min = worldBounds.min.ToXZ();
            Vector2 max = worldBounds.max.ToXZ();
            // 边界落在格线上时 FloorToInt 会漏掉贴边格，向外各扩半格再 clamp
            int minX = Mathf.Clamp(Mathf.FloorToInt((min.x - grid.Origin.x) / grid.CellSize), 0, grid.Width - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt((min.y - grid.Origin.y) / grid.CellSize), 0, grid.Height - 1);
            int maxX = Mathf.Clamp(Mathf.FloorToInt((max.x - grid.Origin.x) / grid.CellSize), 0, grid.Width - 1);
            int maxY = Mathf.Clamp(Mathf.FloorToInt((max.y - grid.Origin.y) / grid.CellSize), 0, grid.Height - 1);

            float checkRadius = AgentRadius;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 center = grid.GetWorldPosition(x, y);
                    bool blocked = Physics.CheckSphere(center.ToWorld(0.5f), checkRadius, _obstacleMask, QueryTriggerInteraction.Ignore);
                    grid.Walkable[grid.GetIndex(x, y)] = !blocked;
                }
            }
        }

        private Bounds CalculateBounds()
        {
            if (_forcedBoundsCenter.HasValue && _forcedBoundsSize.HasValue)
            {
                return new Bounds(_forcedBoundsCenter.Value.ToWorld(), _forcedBoundsSize.Value.ToWorld());
            }

            Vector2 center = _scanCenter ?? Vector2.zero;
            float radius = _scanRadius ?? 10f;
            Vector2 halfSize = Vector2.one * (radius + _margin);

            // y 方向给 2f 覆盖高度，保证扫到地面上的障碍物碰撞体
            Vector3 boxCenter = center.ToWorld(1f);
            Vector3 boxHalfExtents = new Vector3(halfSize.x, 1f, halfSize.y);
            Collider[] obstacles = Physics.OverlapBox(boxCenter, boxHalfExtents, Quaternion.identity, _obstacleMask);

            if (obstacles == null || obstacles.Length == 0)
            {
                Log.Warning("[ColliderGridBuilder] 未在扫描范围内找到任何障碍物，使用扫描范围作为边界");
                return new Bounds(center.ToWorld(), (halfSize * 2f).ToWorld());
            }

            // 以扫描范围为底，再把障碍 bounds 合并进来：障碍入层后网格边界不能只包障碍，
            // 否则远离障碍的出生点/可行走区会被裁掉
            Bounds bounds = new Bounds(center.ToWorld(), (halfSize * 2f).ToWorld());
            foreach (var obstacle in obstacles)
            {
                Bounds obstacleBounds = obstacle.bounds;
                bounds.Encapsulate(obstacleBounds.min.ToXZ().ToWorld());
                bounds.Encapsulate(obstacleBounds.max.ToXZ().ToWorld());
            }

            bounds.Expand(new Vector3(_margin * 2f, 0f, _margin * 2f));
            return bounds;
        }
    }
}
