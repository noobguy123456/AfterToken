using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// GameObject 对象池。
    /// </summary>
    public class GameObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Queue<GameObject> _pool = new Queue<GameObject>();

        public GameObjectPool(GameObject prefab, Transform parent, int preloadCount = 0)
        {
            _prefab = prefab;
            _parent = parent;
            Preload(preloadCount);
        }

        public GameObject Get()
        {
            if (_pool.Count > 0)
            {
                var go = _pool.Dequeue();
                go.SetActive(true);
                return go;
            }

            return Object.Instantiate(_prefab, _parent);
        }

        public void Recycle(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            go.transform.SetParent(_parent);
            _pool.Enqueue(go);
        }

        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var go = _pool.Dequeue();
                if (go != null)
                {
                    Object.Destroy(go);
                }
            }
        }

        private void Preload(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = Object.Instantiate(_prefab, _parent);
                go.SetActive(false);
                _pool.Enqueue(go);
            }
        }
    }
}
