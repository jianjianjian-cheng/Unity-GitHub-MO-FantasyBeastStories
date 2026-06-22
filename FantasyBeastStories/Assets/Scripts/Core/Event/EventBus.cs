using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Event
{
    /// <summary>
    /// 事件总线实现 - 基于字典的事件分发
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _eventHandlers = new Dictionary<Type, List<Delegate>>();
        private readonly Dictionary<int, List<Delegate>> _eventHandlersByHash = new Dictionary<int, List<Delegate>>();
        private bool _isEmitting = false;
        private readonly Queue<Action> _pendingSubscriptions = new Queue<Action>();
        private readonly Queue<Action> _pendingUnsubscriptions = new Queue<Action>();

        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : GameEventBase
        {
            var eventType = typeof(TEvent);
            
            if (_isEmitting)
            {
                _pendingSubscriptions.Enqueue(() => AddHandler(eventType, handler));
            }
            else
            {
                AddHandler(eventType, handler);
            }
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : GameEventBase
        {
            var eventType = typeof(TEvent);
            
            if (_isEmitting)
            {
                _pendingUnsubscriptions.Enqueue(() => RemoveHandler(eventType, handler));
            }
            else
            {
                RemoveHandler(eventType, handler);
            }
        }

        public void Emit<TEvent>(TEvent eventData) where TEvent : GameEventBase
        {
            var eventType = typeof(TEvent);
            
            if (!_eventHandlers.TryGetValue(eventType, out var handlers))
            {
                return;
            }

            _isEmitting = true;

            // 遍历副本以允许订阅/取消订阅
            foreach (var handler in handlers.ToList())
            {
                try
                {
                    ((Action<TEvent>)handler)?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            _isEmitting = false;
            ProcessPendingOperations();
        }

        public void ClearAll()
        {
            _eventHandlers.Clear();
            _eventHandlersByHash.Clear();
            _pendingSubscriptions.Clear();
            _pendingUnsubscriptions.Clear();
        }

        private void AddHandler<TEvent>(Type eventType, Action<TEvent> handler) where TEvent : GameEventBase
        {
            if (!_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType] = new List<Delegate>();
            }
            _eventHandlers[eventType].Add(handler);
            
            // 同时用hash索引，便于快速查找
            int hash = eventType.GetHashCode();
            if (!_eventHandlersByHash.ContainsKey(hash))
            {
                _eventHandlersByHash[hash] = new List<Delegate>();
            }
            _eventHandlersByHash[hash].Add(handler);
        }

        private void RemoveHandler<TEvent>(Type eventType, Action<TEvent> handler) where TEvent : GameEventBase
        {
            if (_eventHandlers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
            }
            
            int hash = eventType.GetHashCode();
            if (_eventHandlersByHash.TryGetValue(hash, out var hashHandlers))
            {
                hashHandlers.Remove(handler);
            }
        }

        private void ProcessPendingOperations()
        {
            while (_pendingSubscriptions.Count > 0)
            {
                _pendingSubscriptions.Dequeue()();
            }
            while (_pendingUnsubscriptions.Count > 0)
            {
                _pendingUnsubscriptions.Dequeue()();
            }
        }
    }
}
