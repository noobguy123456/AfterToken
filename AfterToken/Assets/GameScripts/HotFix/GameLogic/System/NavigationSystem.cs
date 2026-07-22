using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic.Navigation
{
    /// <summary>
    /// 导航系统组件。
    /// 负责管理导航网格、寻路算法、路径缓存。
    /// </summary>
    public class NavigationSystem : MonoBehaviour, INavigationSystem
    {
        public static NavigationSystem Instance { get; private set; }

        [SerializeField] private float _cellSize = 0.5f;
        [SerializeField] private float _margin = 3f;

        private INavigationSystem _navigator;
        private INavigationGridBuilder _gridBuilder;

        private readonly Dictionary<int, PathCacheEntry> _pathCache = new();
        private readonly List<int> _keysToRemove = new List<int>();
        private int _frameCounter;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 设置导航网格（供 INavigationSystem 使用，通常通过 Rebuild 自动设置）。
        /// </summary>
        public void SetGrid(NavigationGrid grid)
        {
            _navigator?.SetGrid(grid);
        }

        /// <summary>
        /// 初始化导航系统。
        /// </summary>
        /// <param name="spawnRadius">敌人生成半径，用于计算网格边界余量。</param>
        /// <param name="origin">网格扫描中心，默认 (0, 0)。</param>
        public void Initialize(float spawnRadius, Vector2? origin = null)
        {
            _margin = Mathf.Max(_margin, spawnRadius + 2f);
            _gridBuilder = new ColliderGridBuilder(
                _cellSize,
                _margin,
                scanCenter: origin,
                scanRadius: spawnRadius + _margin);
            _navigator = new AStarNavigationSystem(_gridBuilder);
            _navigator.Rebuild();
        }

        /// <summary>
        /// 请求路径。优先命中缓存。
        /// </summary>
        public PathResult FindPath(Vector2 from, Vector2 to)
        {
            if (_navigator == null)
            {
                Log.Warning("[NavigationSystem] 导航系统未初始化");
                return PathResult.Failed;
            }

            int cacheKey = GetCacheKey(from, to);
            if (_pathCache.TryGetValue(cacheKey, out var entry))
            {
                if (entry.IsValid(from, to))
                {
                    return entry.Result;
                }
                _pathCache.Remove(cacheKey);
                PathResult.Release(entry.Result);
            }

            var result = _navigator.FindPath(from, to);
            if (result.Success)
            {
                _pathCache[cacheKey] = new PathCacheEntry(from, to, result, Time.time);
            }
            return result;
        }

        /// <summary>
        /// 判断坐标是否可行走。
        /// </summary>
        public bool IsWalkable(Vector2 worldPos)
        {
            return _navigator != null && _navigator.IsWalkable(worldPos);
        }

        /// <summary>
        /// 重新构建网格。
        /// </summary>
        public void Rebuild()
        {
            _navigator?.Rebuild();
            foreach (var entry in _pathCache.Values)
            {
                PathResult.Release(entry.Result);
            }
            _pathCache.Clear();
        }

        private void Update()
        {
            CleanupCache();
        }

        private void CleanupCache()
        {
            _frameCounter++;
            if (_frameCounter % 60 != 0) return;

            float now = Time.time;
            _keysToRemove.Clear();
            foreach (var pair in _pathCache)
            {
                if (now - pair.Value.Timestamp > 2f)
                {
                    _keysToRemove.Add(pair.Key);
                }
            }
            foreach (var key in _keysToRemove)
            {
                if (_pathCache.Remove(key, out var entry))
                {
                    PathResult.Release(entry.Result);
                }
            }
        }

        private int GetCacheKey(Vector2 from, Vector2 to)
        {
            // 粗粒度缓存：坐标按 0.5m 量化，使用 FloorToInt 减少边界抖动
            int fx = Mathf.FloorToInt(from.x * 2f);
            int fy = Mathf.FloorToInt(from.y * 2f);
            int tx = Mathf.FloorToInt(to.x * 2f);
            int ty = Mathf.FloorToInt(to.y * 2f);
            int hash = 17;
            hash = hash * 31 + fx;
            hash = hash * 31 + fy;
            hash = hash * 31 + tx;
            hash = hash * 31 + ty;
            return hash;
        }

        private class PathCacheEntry
        {
            public Vector2 From;
            public Vector2 To;
            public PathResult Result;
            public float Timestamp;

            public PathCacheEntry(Vector2 from, Vector2 to, PathResult result, float timestamp)
            {
                From = from;
                To = to;
                Result = result;
                Timestamp = timestamp;
            }

            public bool IsValid(Vector2 from, Vector2 to)
            {
                return Vector2.Distance(from, From) < 0.5f && Vector2.Distance(to, To) < 0.5f;
            }
        }
    }
}
