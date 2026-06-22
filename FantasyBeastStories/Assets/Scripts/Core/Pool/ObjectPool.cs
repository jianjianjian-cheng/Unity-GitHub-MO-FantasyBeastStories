using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Pool
{
    /// <summary>
    /// Unity对象池实现 - 用于GameObject
    /// </summary>
    public class UnityObjectPool : IUnityPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Queue<GameObject> _available = new Queue<GameObject>();
        private readonly List<GameObject> _active = new List<GameObject>();
        private readonly int _maxSize;
        private readonly bool _expandable;

        public int Count => _available.Count;
        public int ActiveCount => _active.Count;

        public UnityObjectPool(GameObject prefab, Transform parent, int initialSize = 10, int maxSize = 100, bool expandable = true)
        {
            _prefab = prefab;
            _parent = parent;
            _maxSize = maxSize;
            _expandable = expandable;

            // 预创建对象
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        private GameObject CreateNewObject()
        {
            var obj = UnityEngine.Object.Instantiate(_prefab, _parent);
            obj.SetActive(false);
            
            // 尝试获取IPoolable组件并初始化
            if (obj.TryGetComponent<IPoolable>(out var poolable))
            {
                poolable.OnPoolInit();
            }
            
            _available.Enqueue(obj);
            return obj;
        }

        public GameObject Get()
        {
            GameObject obj;

            if (_available.Count > 0)
            {
                obj = _available.Dequeue();
            }
            else if (_expandable && _active.Count < _maxSize)
            {
                obj = CreateNewObject();
                _available.Dequeue(); // 刚创建的还在队列里
            }
            else
            {
                Debug.LogWarning("ObjectPool: No available objects and cannot expand!");
                return null;
            }

            obj.SetActive(true);
            _active.Add(obj);

            // 通知对象被取出
            if (obj.TryGetComponent<IPoolable>(out var poolable))
            {
                poolable.OnPoolGet();
            }

            return obj;
        }

        public void Return(GameObject obj)
        {
            if (obj == null) return;

            obj.SetActive(false);
            obj.transform.SetParent(_parent);
            _active.Remove(obj);
            _available.Enqueue(obj);

            // 通知对象被归还
            if (obj.TryGetComponent<IPoolable>(out var poolable))
            {
                poolable.OnPoolReturn();
            }
        }

        public void Clear()
        {
            foreach (var obj in _active)
            {
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj);
                }
            }
            _active.Clear();

            while (_available.Count > 0)
            {
                var obj = _available.Dequeue();
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj);
                }
            }
        }
    }

    /// <summary>
    /// 泛型对象池 - 用于普通C#对象
    /// </summary>
    public class ObjectPool<T> : IPool<T> where T : class, new()
    {
        private readonly Func<T> _factory;
        private readonly Queue<T> _available = new Queue<T>();
        private readonly HashSet<T> _active = new HashSet<T>();
        private readonly int _maxSize;

        public int Count => _available.Count;
        public int ActiveCount => _active.Count;

        public ObjectPool(Func<T> factory, int initialSize = 10, int maxSize = 100)
        {
            _factory = factory ?? (() => new T());
            _maxSize = maxSize;

            for (int i = 0; i < initialSize; i++)
            {
                _available.Enqueue(_factory());
            }
        }

        public T Get()
        {
            T obj;
            if (_available.Count > 0)
            {
                obj = _available.Dequeue();
            }
            else
            {
                obj = _factory();
            }

            _active.Add(obj);
            return obj;
        }

        public void Return(T obj)
        {
            if (obj == null || !_active.Contains(obj)) return;

            _active.Remove(obj);

            if (_available.Count < _maxSize)
            {
                _available.Enqueue(obj);
            }
        }

        public void Clear()
        {
            _active.Clear();
            _available.Clear();
        }
    }
}
