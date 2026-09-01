using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic.Navigation
{
    /// <summary>
    /// A* 寻路算法实现。
    /// </summary>
    public class AStarNavigationSystem : INavigationSystem
    {
        private NavigationGrid _grid;
        private INavigationGridBuilder _builder;

        private float[] _gCost;
        private float[] _fCost;
        private int[] _parent;
        private int[] _visited;
        // g 值的代数标记：_gCost 复用不清零，未标记本代的格子视为无穷大
        private int[] _gGeneration;
        private int _currentGeneration;
        private readonly SimplePriorityQueue<int> _openQueue = new();
        private readonly HashSet<int> _openSet = new();

        private static readonly int[] Dx = { 0, 0, 1, -1 };
        private static readonly int[] Dy = { 1, -1, 0, 0 };
        private static readonly int ObstacleLayerMask = LayerMask.GetMask("Obstacle");
        // 视线检测球半径略小于代理半径，避免平滑路径穿角/蹭墙，又不至于把合法缝隙误判为阻挡
        private static float LosRadius => ColliderGridBuilder.AgentRadius * 0.9f;

        public AStarNavigationSystem(INavigationGridBuilder builder)
        {
            _builder = builder;
        }

        public void SetGrid(NavigationGrid grid)
        {
            _grid = grid;
            if (grid != null)
            {
                int total = grid.TotalCells;
                _gCost = new float[total];
                _fCost = new float[total];
                _parent = new int[total];
                _visited = new int[total];
                _gGeneration = new int[total];
            }
        }

        public void Rebuild()
        {
            if (_builder == null)
            {
                Log.Warning("[AStarNavigationSystem] 没有网格生成器，无法重建网格");
                return;
            }
            SetGrid(_builder.Build());
        }

        /// <summary>
        /// 局部更新网格：对 worldBounds 覆盖的格子重新判定可走性；网格未初始化时静默忽略。
        /// </summary>
        public void UpdateRegion(Bounds worldBounds)
        {
            if (_grid == null || _builder == null) return;
            _builder.UpdateRegion(_grid, worldBounds);
        }

        public bool IsWalkable(Vector2 worldPos)
        {
            return _grid != null && _grid.IsWalkable(worldPos);
        }

        public PathResult FindPath(Vector2 from, Vector2 to)
        {
            if (_grid == null)
            {
                Log.Warning("[AStarNavigationSystem] 导航网格未初始化");
                return PathResult.Failed;
            }

            if (!_grid.TryGetGridCoordinates(from, out int startX, out int startY) ||
                !_grid.IsWalkable(startX, startY))
            {
                // 起点在不可行走区域，尝试找邻近可行走点
                if (!FindNearestWalkable(from, out startX, out startY))
                    return PathResult.Failed;
            }

            if (!_grid.TryGetGridCoordinates(to, out int endX, out int endY) ||
                !_grid.IsWalkable(endX, endY))
            {
                // 终点不可达，尝试找邻近可行走点
                if (!FindNearestWalkable(to, out endX, out endY))
                    return PathResult.Failed;
            }

            int startIndex = _grid.GetIndex(startX, startY);
            int endIndex = _grid.GetIndex(endX, endY);

            if (startIndex == endIndex)
            {
                var directResult = PathResult.Acquire();
                directResult.Success = true;
                directResult.Waypoints.Add(to);
                return directResult;
            }

            _currentGeneration++;
            if (_currentGeneration <= 0) // 防止 int 溢出
            {
                _currentGeneration = 1;
                System.Array.Clear(_visited, 0, _visited.Length);
                System.Array.Clear(_gGeneration, 0, _gGeneration.Length);
            }
            _openQueue.Clear();
            _openSet.Clear();

            _gCost[startIndex] = 0;
            _gGeneration[startIndex] = _currentGeneration;
            _fCost[startIndex] = Heuristic(startX, startY, endX, endY);
            _parent[startIndex] = -1;
            _openQueue.Enqueue(startIndex, _fCost[startIndex]);
            _openSet.Add(startIndex);

            while (_openQueue.Count > 0)
            {
                int currentIndex = _openQueue.Dequeue();
                _openSet.Remove(currentIndex);

                if (currentIndex == endIndex)
                {
                    return ReconstructPath(startIndex, endIndex, to);
                }

                _visited[currentIndex] = _currentGeneration;
                int cx = currentIndex % _grid.Width;
                int cy = currentIndex / _grid.Width;

                for (int i = 0; i < 4; i++)
                {
                    int nx = cx + Dx[i];
                    int ny = cy + Dy[i];

                    if (!_grid.IsInside(nx, ny)) continue;
                    if (!_grid.IsWalkable(nx, ny)) continue;

                    int neighborIndex = _grid.GetIndex(nx, ny);
                    if (_visited[neighborIndex] == _currentGeneration) continue;

                    float tentativeG = _gCost[currentIndex] + CellDistance;
                    // 本代未写入过 g 值的格子视为无穷大，直接接受新路径
                    if (_gGeneration[neighborIndex] != _currentGeneration || tentativeG < _gCost[neighborIndex])
                    {
                        _gGeneration[neighborIndex] = _currentGeneration;
                        _gCost[neighborIndex] = tentativeG;
                        _fCost[neighborIndex] = tentativeG + Heuristic(nx, ny, endX, endY);
                        _parent[neighborIndex] = currentIndex;

                        if (!_openSet.Contains(neighborIndex))
                        {
                            _openQueue.Enqueue(neighborIndex, _fCost[neighborIndex]);
                            _openSet.Add(neighborIndex);
                        }
                    }
                }
            }

            return PathResult.Failed;
        }

        private PathResult ReconstructPath(int startIndex, int endIndex, Vector2 targetWorldPos)
        {
            var result = PathResult.Acquire();
            result.Success = true;
            var path = result.Waypoints;
            path.Clear();

            int current = endIndex;
            while (current != -1 && current != startIndex)
            {
                path.Add(_grid.GetWorldPosition(current % _grid.Width, current / _grid.Width));
                current = _parent[current];
            }

            // 加入起点
            if (current == startIndex)
            {
                path.Add(_grid.GetWorldPosition(current % _grid.Width, current / _grid.Width));
            }

            path.Reverse();

            // 用目标位置替换最后一个路径点，避免网格中心点偏差
            if (path.Count > 0)
            {
                path[path.Count - 1] = targetWorldPos;
            }

            SmoothPathInPlace(path);
            result.PathLength = CalculatePathLength(path);
            return result;
        }

        private void SmoothPathInPlace(List<Vector2> path)
        {
            if (path.Count <= 2) return;

            int current = 0;
            while (current < path.Count - 1)
            {
                int furthest = current + 1;
                for (int i = current + 2; i < path.Count; i++)
                {
                    if (HasLineOfSight(path[current], path[i]))
                    {
                        furthest = i;
                    }
                    else
                    {
                        break;
                    }
                }

                if (furthest > current + 1)
                {
                    path.RemoveRange(current + 1, furthest - current - 1);
                }
                current++;
            }
        }

        private bool HasLineOfSight(Vector2 from, Vector2 to)
        {
            Vector2 direction = to - from;
            float distance = direction.magnitude;
            if (distance < 0.001f) return true;

            // 带宽度检测（忽略 trigger），避免零宽 Linecast 平滑出穿角/蹭墙的路径
            Vector3 origin = from.ToWorld(0.5f);
            return !Physics.SphereCast(origin, LosRadius, direction.normalized.ToWorld(), out _, distance, ObstacleLayerMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// 找世界坐标附近最近的可行走格中心，用于出生点吸附等兜底。
        /// </summary>
        public bool TryGetNearestWalkable(Vector2 worldPos, out Vector2 walkablePos)
        {
            walkablePos = worldPos;
            if (FindNearestWalkable(worldPos, out int x, out int y))
            {
                walkablePos = _grid.GetWorldPosition(x, y);
                return true;
            }
            return false;
        }

        private float CalculatePathLength(List<Vector2> waypoints)
        {
            float length = 0;
            for (int i = 1; i < waypoints.Count; i++)
            {
                length += Vector2.Distance(waypoints[i - 1], waypoints[i]);
            }
            return length;
        }

        private bool FindNearestWalkable(Vector2 worldPos, out int x, out int y)
        {
            x = 0;
            y = 0;
            if (_grid == null) return false;
            if (!_grid.TryGetGridCoordinates(worldPos, out int cx, out int cy)) return false;

            //  spiral 搜索最近可行走格子
            if (_grid.IsWalkable(cx, cy))
            {
                x = cx;
                y = cy;
                return true;
            }

            int maxRadius = Mathf.Max(_grid.Width, _grid.Height);
            for (int r = 1; r < maxRadius; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                        int nx = cx + dx;
                        int ny = cy + dy;
                        if (_grid.IsWalkable(nx, ny))
                        {
                            x = nx;
                            y = ny;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private float Heuristic(int x1, int y1, int x2, int y2)
        {
            // 曼哈顿距离，适配 4 方向移动
            return Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2);
        }

        private float CellDistance => _grid?.CellSize ?? 1f;
    }
}
