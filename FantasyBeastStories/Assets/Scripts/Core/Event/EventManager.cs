using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Event
{
    /// <summary>
    /// 全局事件管理器 - 继承自MonoSingleton，提供全局事件访问
    /// </summary>
    public class EventManager : MonoSingleton<EventManager>
    {
        private IEventBus _eventBus;
        
        public IEventBus EventBus => _eventBus ??= new EventBus();

        protected override void Awake()
        {
            base.Awake();
            _eventBus = new EventBus();
        }

        // 便捷方法
        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : GameEventBase
        {
            EventBus.Subscribe(handler);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : GameEventBase
        {
            EventBus.Unsubscribe(handler);
        }

        public void Emit<TEvent>(TEvent eventData) where TEvent : GameEventBase
        {
            EventBus.Emit(eventData);
        }

        public void ClearAll()
        {
            EventBus.ClearAll();
        }
    }
}
