using System;
using UnityEngine;

namespace Core.EventSystem
{
    /// <summary>
    /// 事件通道的MonoBehaviour扩展方法
    /// 提供便捷的自动注册/注销功能
    /// </summary>
    public static class EventChannelExtensions
    {
        /// <summary>
        /// 注册事件处理函数，自动在OnDisable时注销
        /// </summary>
        public static void RegisterWithAutoUnregister<T>(
            this EventChannelSO<T> channel,
            MonoBehaviour subscriber,
            Action<T> handler) where T : GameEventArgs
        {
            if (channel == null || subscriber == null || handler == null)
                return;
                
            channel.Register(handler);
            subscriber.StartCoroutine(AutoUnregisterCoroutine(channel, subscriber, handler));
        }
        
        /// <summary>
        /// 注册无参数事件处理函数，自动在OnDisable时注销
        /// </summary>
        public static void RegisterWithAutoUnregister(
            this VoidEventChannelSO channel,
            MonoBehaviour subscriber,
            Action handler)
        {
            if (channel == null || subscriber == null || handler == null)
                return;
                
            channel.Register(handler);
            subscriber.StartCoroutine(AutoUnregisterCoroutine(channel, subscriber, handler));
        }
        
        private static System.Collections.IEnumerator AutoUnregisterCoroutine<T>(
            EventChannelSO<T> channel,
            MonoBehaviour subscriber,
            Action<T> handler) where T : GameEventArgs
        {
            yield return new WaitUntil(() => subscriber == null || !subscriber.isActiveAndEnabled);
            if (channel != null)
            {
                channel.Unregister(handler);
            }
        }
        
        private static System.Collections.IEnumerator AutoUnregisterCoroutine(
            VoidEventChannelSO channel,
            MonoBehaviour subscriber,
            Action handler)
        {
            yield return new WaitUntil(() => subscriber == null || !subscriber.isActiveAndEnabled);
            if (channel != null)
            {
                channel.Unregister(handler);
            }
        }
    }
}
