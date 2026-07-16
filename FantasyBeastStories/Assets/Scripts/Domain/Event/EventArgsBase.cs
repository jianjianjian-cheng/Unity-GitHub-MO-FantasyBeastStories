using System;

namespace Domain.Event
{
    /// <summary>
    /// 所有事件参数的抽象基类，统一事件数据规范。
    /// 所有自定义事件数据类都应继承此类。
    /// </summary>
    public abstract class EventArgsBase
    {
        /// <summary>
        /// 事件创建时的时间戳（UTC），用于事件溯源和调试。
        /// </summary>
        public DateTime Timestamp { get; protected set; }

        public string EventType => GetType().Name;

        protected EventArgsBase()
        {
            Timestamp = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"[{EventType}] Timestamp={Timestamp:yyyy-MM-dd HH:mm:ss.fff}";
        }
    }
}