using System;

namespace Core.Event
{
    /// <summary>
    /// 事件总线接口 - 解耦事件发布和订阅
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 订阅事件
        /// </summary>
        void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : GameEventBase;

        /// <summary>
        /// 取消订阅
        /// </summary>
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : GameEventBase;

        /// <summary>
        /// 发布事件
        /// </summary>
        void Emit<TEvent>(TEvent eventData) where TEvent : GameEventBase;

        /// <summary>
        /// 清除所有事件订阅
        /// </summary>
        void ClearAll();
    }
}
