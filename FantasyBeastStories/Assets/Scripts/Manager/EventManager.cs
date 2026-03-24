using System;
using System.Collections;
using System.Collections.Generic;
using Events;
using UnityEngine;

namespace Manager
{
    public class EventManager : MonoBehaviour
    {
        #region 单例模式
        [SerializeField] private bool isTest; // 是否测试模式
        //事件管理器(单例)
        public static EventManager instance;
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

        #endregion

        void Start()
        {
            if (isTest)
            {

            }
        }

        /// <summary>
        /// 事件字典
        /// </summary>
        /// <remarks> 无参数事件字典
        private Dictionary<string, Action> eventDictionary = new Dictionary<string, Action>();
        /// </remarks> 复杂参数事件字典
        private Dictionary<string, Action<EventArgsBase>> eventDictionaryComplex = new Dictionary<string, Action<EventArgsBase>>();

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

        /// <summary> 注册复杂事件
        /// </summary>
        public void RegisterEventComplex(string eventName, Action<EventArgsBase> action)
        {
            if (eventDictionaryComplex.ContainsKey(eventName))
            {
                eventDictionaryComplex[eventName] += action;
            }
            else
            {
                eventDictionaryComplex.Add(eventName, action);
            }
        }
        /// <summary> 注销复杂事件
        /// </summary>
        public void UnRegisterEventComplex(string eventName, Action<EventArgsBase> action)
        {
            if (eventDictionaryComplex.ContainsKey(eventName))
            {
                eventDictionaryComplex[eventName] -= action;
            }
        }
        /// <summary> 触发复杂事件
        /// </summary>
        public void TriggerEventComplex(string eventName, EventArgsBase args)
        {
            if (eventDictionaryComplex.ContainsKey(eventName))
            {
                eventDictionaryComplex[eventName]?.Invoke(args);
            }
        }
    }

    public class EventNames
    {
        public const string DamageReceived = "DamageReceived";
    }
}
