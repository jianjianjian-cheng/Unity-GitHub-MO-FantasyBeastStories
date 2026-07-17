using System;
using System.Collections.Generic;
using UnityEngine;

namespace Controllers.Services
{
    /// <summary>
    /// Domain 层服务注册器
    /// 用途：替代 Application.ServiceLocator，使 Domain 层不依赖 Application 层
    /// 
    /// 使用方式：
    /// - 在 Application/Infrastructure 层的 Awake/Start 中 Register 实现
    /// - Domain 层内部通过 Get/TryGet 获取服务
    /// </summary>
    public static class DomainServiceLocator
    {
        private static Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            _services[type] = service;
        }

        public static void Unregister<T>() where T : class
        {
            _services.Remove(typeof(T));
        }

        public static T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
                return service as T;
            return null;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var s))
            {
                service = s as T;
                return true;
            }
            service = null;
            return false;
        }
    }
}