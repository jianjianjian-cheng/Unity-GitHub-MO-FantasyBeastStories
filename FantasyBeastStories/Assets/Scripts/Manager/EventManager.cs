using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manager
{
    public class EventManager : MonoBehaviour
    {
        //事件管理器(单例)
        private static EventManager instance;
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 事件字典
        /// </summary>
        private Dictionary<string, Action> eventDictionary = new Dictionary<string, Action>();

        /// <summary>
        /// 注册事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="action">事件处理函数</param>
        public void RegisterEvent(string eventName, Action action)
        {
            if (eventDictionary.ContainsKey(eventName))
            {
                eventDictionary[eventName] += action;
            }
            else
            {
                eventDictionary.Add(eventName, action);
            }
        }
        /// <summary>
        /// 注销事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="action">事件处理函数</param>
        public void UnRegisterEvent(string eventName, Action action)
        {
            if (eventDictionary.ContainsKey(eventName))
            {
                eventDictionary[eventName] -= action;
            }
        }
        /// <summary>
        /// 触发事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        public void TriggerEvent(string eventName)
        {
            if (eventDictionary.ContainsKey(eventName))
            {
                eventDictionary[eventName]?.Invoke();
            }
        }
    }
}
