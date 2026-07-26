using System;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public static class ServiceLocator
    {
        private static Dictionary<Type, object> _services = new();

        public static void Register<T>(T service) where T : class
        {
            _services[typeof(T)] = service;
        }

        public static void Unregister<T>() where T : class
        {
            _services.Remove(typeof(T));
        }

        /// <summary>获取服务，未注册时返回 null（不报错）</summary>
        public static T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
                return service as T;
            return null;
        }

        /// <summary>获取服务，未注册时报错（用于必须存在的服务）</summary>
        public static T GetRequired<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
                return service as T;
            Debug.LogError($"Service {typeof(T)} not registered");
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