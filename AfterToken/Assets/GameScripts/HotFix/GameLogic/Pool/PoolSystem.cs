using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 对象池管理器。
    /// </summary>
    public class PoolSystem : MonoBehaviour
    {
        public static PoolSystem Instance { get; private set; }

        private readonly Dictionary<string, GameObjectPool> _pools = new Dictionary<string, GameObjectPool>();

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// 从对象池获取 GameObject。
        /// </summary>
        public GameObject Get(string key, GameObject prefab, Transform parent, int preloadCount = 0)
        {
            if (!_pools.TryGetValue(key, out var pool))
            {
                pool = new GameObjectPool(prefab, parent, preloadCount);
                _pools[key] = pool;
            }

            return pool.Get();
        }

        /// <summary>
        /// 回收 GameObject。
        /// </summary>
        public void Recycle(string key, GameObject go)
        {
            if (_pools.TryGetValue(key, out var pool))
            {
                pool.Recycle(go);
            }
            else if (go != null)
            {
                Destroy(go);
            }
        }

        /// <summary>
        /// 清空指定对象池。
        /// </summary>
        public void Clear(string key)
        {
            if (_pools.TryGetValue(key, out var pool))
            {
                pool.Clear();
                _pools.Remove(key);
            }
        }

        /// <summary>
        /// 清空所有对象池。
        /// </summary>
        public void ClearAll()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }

            _pools.Clear();
        }
    }
}
