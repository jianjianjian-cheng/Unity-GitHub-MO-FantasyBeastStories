using System;
using UnityEngine;

namespace Core.Pool
{
    /// <summary>
    /// 对象池接口
    /// </summary>
    public interface IPool<T> where T : class
    {
        T Get();
        void Return(T obj);
        int Count { get; }
        int ActiveCount { get; }
        void Clear();
    }

    /// <summary>
    /// 可回收接口 - 对象池中的对象实现此接口
    /// </summary>
    public interface IPoolable
    {
        void OnPoolInit();
        void OnPoolGet();
        void OnPoolReturn();
    }

    /// <summary>
    /// Unity对象池接口
    /// </summary>
    public interface IUnityPool
    {
        GameObject Get();
        void Return(GameObject obj);
        int Count { get; }
        int ActiveCount { get; }
        void Clear();
    }
}
