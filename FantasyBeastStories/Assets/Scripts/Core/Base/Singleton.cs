using System;

namespace Core
{
    /// <summary>
    /// 单例模式基类 - 线程安全，使用双检锁
    /// </summary>
    public abstract class Singleton<T> where T : class
    {
        private static readonly object _lock = new object();
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = CreateInstance();
                        }
                    }
                }
                return _instance;
            }
        }

        private static T CreateInstance()
        {
            var constructor = typeof(T).GetConstructor(
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Public,
                null, new Type[0], null);
            
            if (constructor != null)
            {
                return constructor.Invoke(null) as T;
            }
            
            throw new InvalidOperationException($"Type {typeof(T).Name} must have a private constructor.");
        }

        protected virtual void OnSingletonInit() { }
    }
}
