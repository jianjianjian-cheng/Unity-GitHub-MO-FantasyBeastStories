using System.Collections.Generic;
using UnityEngine;

namespace Core.Pool
{
    /// <summary>
    /// 对象池管理器 - 管理多种对象池
    /// </summary>
    public class PoolManager : MonoSingleton<PoolManager>
    {
        private readonly Dictionary<string, IUnityPool> _pools = new Dictionary<string, IUnityPool>();
        private readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();

        [SerializeField] private Transform _poolRoot;

        protected override void Awake()
        {
            base.Awake();
            
            if (_poolRoot == null)
            {
                var poolRootObj = new GameObject("[PoolRoot]");
                _poolRoot = poolRootObj.transform;
                _poolRoot.SetParent(transform);
            }
        }

        /// <summary>
        /// 注册对象池
        /// </summary>
        public void Register(string poolId, GameObject prefab, int initialSize = 10, int maxSize = 100, bool expandable = true)
        {
            if (_pools.ContainsKey(poolId))
            {
                Debug.LogWarning($"Pool '{poolId}' already registered!");
                return;
            }

            _prefabs[poolId] = prefab;
            _pools[poolId] = new UnityObjectPool(prefab, _poolRoot, initialSize, maxSize, expandable);
        }

        /// <summary>
        /// 获取对象
        /// </summary>
        public GameObject Get(string poolId)
        {
            if (!_pools.TryGetValue(poolId, out var pool))
            {
                Debug.LogError($"Pool '{poolId}' not found! Please register it first.");
                return null;
            }

            return pool.Get();
        }

        /// <summary>
        /// 归还对象
        /// </summary>
        public void Return(string poolId, GameObject obj)
        {
            if (!_pools.TryGetValue(poolId, out var pool))
            {
                Debug.LogError($"Pool '{poolId}' not found!");
                return;
            }

            pool.Return(obj);
        }

        /// <summary>
        /// 获取或创建对象池
        /// </summary>
        public GameObject GetOrCreate(string poolId, GameObject prefab, int initialSize = 10, int maxSize = 100)
        {
            if (!_pools.ContainsKey(poolId))
            {
                Register(poolId, prefab, initialSize, maxSize);
            }

            return Get(poolId);
        }

        /// <summary>
        /// 预加载对象到池中
        /// </summary>
        public void Preload(string poolId, int count)
        {
            if (!_pools.TryGetValue(poolId, out var pool))
            {
                Debug.LogError($"Pool '{poolId}' not found!");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                pool.Return(pool.Get());
            }
        }

        /// <summary>
        /// 清除指定池
        /// </summary>
        public void Clear(string poolId)
        {
            if (_pools.TryGetValue(poolId, out var pool))
            {
                pool.Clear();
            }
        }

        /// <summary>
        /// 清除所有池
        /// </summary>
        public void ClearAll()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }
            _pools.Clear();
            _prefabs.Clear();
        }

        /// <summary>
        /// 获取池的统计信息
        /// </summary>
        public (int available, int active) GetPoolStats(string poolId)
        {
            if (_pools.TryGetValue(poolId, out var pool))
            {
                return (pool.Count, pool.ActiveCount);
            }
            return (0, 0);
        }
    }
}
