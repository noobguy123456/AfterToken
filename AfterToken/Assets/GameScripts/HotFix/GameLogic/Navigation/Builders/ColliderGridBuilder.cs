using TEngine;
using UnityEngine;

namespace GameLogic.Navigation
{
    /// <summary>
    /// 基于 Collider2D 自动生成导航网格。
    /// </summary>
    public class ColliderGridBuilder : INavigationGridBuilder
    {
        private readonly float _cellSize;
        private readonly float _margin;
        private readonly LayerMask _obstacleMask;
        private readonly Vector2? _forcedBoundsCenter;
        private readonly Vector2? _forcedBoundsSize;

        public ColliderGridBuilder(
            float cellSize = 0.5f,
            float margin = 2f,
            LayerMask? obstacleMask = null,
            Vector2? forcedBoundsCenter = null,
            Vector2? forcedBoundsSize = null)
        {
            _cellSize = cellSize;
            _margin = margin;
            _obstacleMask = obstacleMask ?? LayerMask.GetMask("Obstacle");
            _forcedBoundsCenter = forcedBoundsCenter;
            _forcedBoundsSize = forcedBoundsSize;
        }

        public NavigationGrid Build()
        {
            Bounds bounds = CalculateBounds();
            Vector2 min = bounds.min;
            Vector2 max = bounds.max;

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

            Vector2 halfCell = Vector2.one * (_cellSize * 0.5f);
            float checkRadius = _cellSize * 0.25f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 center = grid.GetWorldPosition(x, y);
                    // 使用小圆检测格子中心是否被障碍物覆盖
                    bool blocked = Physics2D.OverlapCircle(center, checkRadius, _obstacleMask) != null;
                    grid.Walkable[grid.GetIndex(x, y)] = !blocked;
                }
            }

            return grid;
        }

        private Bounds CalculateBounds()
        {
            if (_forcedBoundsCenter.HasValue && _forcedBoundsSize.HasValue)
            {
                return new Bounds((Vector3)_forcedBoundsCenter.Value, (Vector3)_forcedBoundsSize.Value);
            }

            Collider2D[] obstacles = Physics2D.OverlapAreaAll(
                Vector2.one * -1000f,
                Vector2.one * 1000f,
                _obstacleMask);

            if (obstacles == null || obstacles.Length == 0)
            {
                Log.Warning("[ColliderGridBuilder] 未找到任何障碍物，使用默认边界 (-10, -10) ~ (10, 10)");
                return new Bounds(Vector3.zero, new Vector3(20f, 20f, 1f));
            }

            Bounds bounds = obstacles[0].bounds;
            for (int i = 1; i < obstacles.Length; i++)
            {
                bounds.Encapsulate(obstacles[i].bounds);
            }

            bounds.Expand(new Vector3(_margin * 2f, _margin * 2f, 0f));
            return bounds;
        }
    }
}
