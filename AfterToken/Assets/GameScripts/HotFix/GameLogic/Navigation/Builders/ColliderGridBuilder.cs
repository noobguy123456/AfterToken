using TEngine;
using UnityEngine;

namespace GameLogic.Navigation
{
    /// <summary>
    /// 基于 Collider 自动生成导航网格。
    /// </summary>
    public class ColliderGridBuilder : INavigationGridBuilder
    {
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

            float checkRadius = _cellSize * 0.25f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 center = grid.GetWorldPosition(x, y);
                    // 检测高度取 0.5f，与地面上的障碍物碰撞体对齐
                    bool blocked = Physics.CheckSphere(center.ToWorld(0.5f), checkRadius, _obstacleMask);
                    grid.Walkable[grid.GetIndex(x, y)] = !blocked;
                }
            }

            return grid;
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

            // 障碍 bounds 只关心玩法平面 (x, z)，压缩 y 后合并，避免障碍物高度撑大网格边界
            Bounds bounds = new Bounds(obstacles[0].bounds.min.ToXZ().ToWorld(), Vector3.zero);
            bounds.Encapsulate(obstacles[0].bounds.max.ToXZ().ToWorld());
            for (int i = 1; i < obstacles.Length; i++)
            {
                Bounds obstacleBounds = obstacles[i].bounds;
                bounds.Encapsulate(obstacleBounds.min.ToXZ().ToWorld());
                bounds.Encapsulate(obstacleBounds.max.ToXZ().ToWorld());
            }

            bounds.Expand(new Vector3(_margin * 2f, 0f, _margin * 2f));
            return bounds;
        }
    }
}
