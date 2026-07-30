using UnityEngine;

namespace Core
{
    /// <summary>
    /// 对象池热路径静态辅助类
    /// 绕过事件通道（PoolOperationData + lambda 委托），
    /// 直接调用 ObjectPoolManager，消除每次池操作的堆分配
    /// </summary>
    public static class PoolHelper
    {
        private static ObjectPoolManager _mgr;

        public static ObjectPoolManager Mgr
        {
            get
            {
                if (_mgr == null)
                    _mgr = ServiceLocator.Get<ObjectPoolManager>();
                return _mgr;
            }
        }

        /// <summary>从对象池获取并激活（零 GC 分配）</summary>
        public static GameObject Get(string poolName, Vector3? position = null)
        {
            return Mgr?.GetFromPoolAndActivate(poolName, position);
        }

        /// <summary>归还到对象池（零 GC 分配）</summary>
        public static void Return(string poolName, GameObject obj)
        {
            Mgr?.ReturnToPool(poolName, obj);
        }
    }
}