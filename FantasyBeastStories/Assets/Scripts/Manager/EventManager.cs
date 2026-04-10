using System;
using System.Collections;
using System.Collections.Generic;
using Atttibute;
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
        //返回AttributePlayerBase组件字典
        private Dictionary<string, AttributePlayerBase> attributePlayerBaseDictionary = new Dictionary<string, AttributePlayerBase>();
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
        /// <summary>
        /// 注册AttributePlayerBase组件
        /// </summary>
        /// <param name="playerName">玩家名称</param>
        /// <param name="attributePlayerBase">AttributePlayerBase组件</param>
        public void RegisterAttributePlayerBase(string playerName, AttributePlayerBase attributePlayerBase)
        {
            if (attributePlayerBaseDictionary.ContainsKey(playerName))
            {
                attributePlayerBaseDictionary[playerName] = attributePlayerBase;
            }
            else
            {
                attributePlayerBaseDictionary.Add(playerName, attributePlayerBase);
            }
        }
        /// <summary>
        /// 注销AttributePlayerBase组件
        /// </summary>
        /// <param name="playerName">玩家名称</param>
        public void UnRegisterAttributePlayerBase(string playerName)
        {
            if (attributePlayerBaseDictionary.ContainsKey(playerName))
            {
                attributePlayerBaseDictionary.Remove(playerName);
            }
        }
        /// <summary>
        /// 获取AttributePlayerBase组件
        /// </summary>
        /// <param name="playerName">玩家名称</param>
        /// <returns>AttributePlayerBase组件</returns>
        public AttributePlayerBase GetAttributePlayerBase(string playerName)
        {
            if (attributePlayerBaseDictionary.ContainsKey(playerName))
            {
                return attributePlayerBaseDictionary[playerName];
            }
            else
            {
                return null;
            }
        }
    }

    public class EventNames
    {
        public const string DamageReceived = "DamageReceived";
        public const string UpdateAttributePlayer = "UpdateAttributePlayer";
        public const string RuneInfo = "RuneInfo";
    }
}
