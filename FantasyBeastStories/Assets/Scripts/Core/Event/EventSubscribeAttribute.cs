using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Core.Event
{
    /// <summary>
    /// 事件订阅特性 - 标记方法自动订阅事件
    /// 使用方法：在要订阅事件的方法上添加 [EventSubscribe(typeof(YourEvent))]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class EventSubscribeAttribute : Attribute
    {
        public Type EventType { get; }
        public int Priority { get; }

        public EventSubscribeAttribute(Type eventType, int priority = 0)
        {
            if (!typeof(GameEventBase).IsAssignableFrom(eventType))
            {
                throw new ArgumentException($"EventType must inherit from GameEventBase", nameof(eventType));
            }
            EventType = eventType;
            Priority = priority;
        }
    }

    /// <summary>
    /// 事件订阅器 - 自动管理基于特性的事件订阅
    /// </summary>
    public class EventSubscriber
    {
        private static readonly Dictionary<MethodInfo, EventSubscribeAttribute> _cachedAttributes = new Dictionary<MethodInfo, EventSubscribeAttribute>();

        public static void Subscribe(object target, IEventBus eventBus)
        {
            var type = target.GetType();
            var methods = type.GetMethods(System.Reflection.BindingFlags.Instance | 
                                          System.Reflection.BindingFlags.Public | 
                                          System.Reflection.BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                if (_cachedAttributes.TryGetValue(method, out var attribute))
                {
                    SubscribeMethod(target, method, attribute, eventBus);
                }
                else
                {
                    var attrs = method.GetCustomAttributes(typeof(EventSubscribeAttribute), false);
                    if (attrs.Length > 0)
                    {
                        var attr = (EventSubscribeAttribute)attrs[0];
                        _cachedAttributes[method] = attr;
                        SubscribeMethod(target, method, attr, eventBus);
                    }
                }
            }
        }

        public static void Unsubscribe(object target, IEventBus eventBus)
        {
            var type = target.GetType();
            var methods = type.GetMethods(System.Reflection.BindingFlags.Instance | 
                                          System.Reflection.BindingFlags.Public | 
                                          System.Reflection.BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                if (_cachedAttributes.TryGetValue(method, out var attribute))
                {
                    UnsubscribeMethod(target, method, attribute, eventBus);
                }
            }
        }

        private static void SubscribeMethod(object target, System.Reflection.MethodInfo method, 
            EventSubscribeAttribute attr, IEventBus eventBus)
        {
            var eventType = attr.EventType;
            var genericHandlerType = typeof(Action<>).MakeGenericType(eventType);
            var handler = Delegate.CreateDelegate(genericHandlerType, target, method);
            
            var subscribeMethod = typeof(IEventBus).GetMethod(nameof(IEventBus.Subscribe));
            var genericSubscribe = subscribeMethod.MakeGenericMethod(eventType);
            genericSubscribe.Invoke(eventBus, new object[] { handler });
        }

        private static void UnsubscribeMethod(object target, System.Reflection.MethodInfo method, 
            EventSubscribeAttribute attr, IEventBus eventBus)
        {
            var eventType = attr.EventType;
            var genericHandlerType = typeof(Action<>).MakeGenericType(eventType);
            var handler = Delegate.CreateDelegate(genericHandlerType, target, method);
            
            var unsubscribeMethod = typeof(IEventBus).GetMethod(nameof(IEventBus.Unsubscribe));
            var genericUnsubscribe = unsubscribeMethod.MakeGenericMethod(eventType);
            genericUnsubscribe.Invoke(eventBus, new object[] { handler });
        }
    }

    internal static class MethodInfoExtensions
    {
        public static System.Reflection.MethodInfo GetMethod(this Type type, string name)
        {
            return type.GetMethod(name);
        }
    }
}
