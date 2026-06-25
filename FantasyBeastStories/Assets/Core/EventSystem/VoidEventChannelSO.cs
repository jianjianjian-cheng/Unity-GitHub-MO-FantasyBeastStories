using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.EventSystem
{
    /// <summary>
    /// 无参数事件通道
    /// 用于不需要传递参数的事件
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Void Event Channel")]
    public class VoidEventChannelSO : ScriptableObject
    {
        private HashSet<Action> _handlers = new HashSet<Action>();
        
        /// <summary>
        /// 注册事件处理函数
        /// </summary>
        /// <param name="handler">处理函数</param>
        public void Register(Action handler)
        {
            if (handler != null)
            {
                _handlers.Add(handler);
            }
        }
        
        /// <summary>
        /// 注销事件处理函数
        /// </summary>
        /// <param name="handler">处理函数</param>
        public void Unregister(Action handler)
        {
            _handlers.Remove(handler);
        }
        
        /// <summary>
        /// 发布事件
        /// </summary>
        public void Raise()
        {
            // 复制列表以避免在迭代过程中修改集合
            var handlersCopy = new HashSet<Action>(_handlers);
            foreach (var handler in handlersCopy)
            {
                try
                {
                    handler?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
        
        /// <summary>
        /// 注销所有处理函数
        /// </summary>
        public void Clear()
        {
            _handlers.Clear();
        }
        
        /// <summary>
        /// 获取已注册的处理函数数量
        /// </summary>
        public int HandlerCount => _handlers.Count;
    }
}
