using System;
using System.Collections;
using System.Collections.Generic;
using Atttibute;
using CardData;
using Events;
using Photon.Pun;
using UnityEngine;

namespace Manager
{
    public class EventManager : MonoBehaviour
    {
        #region 单例模式
        [SerializeField]
        private bool isTest; // 是否测试模式

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
            if (isTest) { }
        }

        /// <summary>
        /// 事件字典
        /// </summary>
        /// <remarks> 无参数事件字典
        private Dictionary<string, Action> eventDictionary = new Dictionary<string, Action>();

        /// </remarks> 复杂参数事件字典
        private Dictionary<string, Action<EventArgsBase>> eventDictionaryComplex =
            new Dictionary<string, Action<EventArgsBase>>();
        private Dictionary<
            (int actorNumber, string key),
            AttributePlayerBase
        > attributePlayerBaseDictionary = new Dictionary<(int, string), AttributePlayerBase>();

        //返回bool组件字典
        private Dictionary<string, bool> boolDictionary = new Dictionary<string, bool>();

        //bool参数事件字典
        private Dictionary<string, Action<bool>> boolEventDictionary =
            new Dictionary<string, Action<bool>>();

        //用于回收触发器和特效分开处理的攻击
        private Dictionary<string, Action> attackEventDictionary = new Dictionary<string, Action>();

        //双浮点数参数字典
        private Dictionary<string, Action<float, float>> floatEventDictionary =
            new Dictionary<string, Action<float, float>>();

        //CardConfigBase参数字典
        private Dictionary<string, Action<CardConfigBase>> cardConfigDictionary =
            new Dictionary<string, Action<CardConfigBase>>();

        #region  事件相关的方法

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
        /// <param name="actorNumber">玩家ActorNumber（Photon唯一标识）</param>
        /// <param name="key">属性Key（如"MainPlayer"、"Enemy"等）</param>
        /// <param name="attributePlayerBase">AttributePlayerBase组件</param>
        public void RegisterAttributePlayerBase(
            int actorNumber,
            string key,
            AttributePlayerBase attributePlayerBase
        )
        {
            var dictKey = (actorNumber, key);
            if (attributePlayerBaseDictionary.ContainsKey(dictKey))
            {
                attributePlayerBaseDictionary[dictKey] = attributePlayerBase;
            }
            else
            {
                attributePlayerBaseDictionary.Add(dictKey, attributePlayerBase);
            }
        }

        /// <summary>
        /// 注销AttributePlayerBase组件
        /// </summary>
        public void UnRegisterAttributePlayerBase(int actorNumber, string key)
        {
            var dictKey = (actorNumber, key);
            if (attributePlayerBaseDictionary.ContainsKey(dictKey))
            {
                attributePlayerBaseDictionary.Remove(dictKey);
            }
        }

        /// <summary>
        /// 获取AttributePlayerBase组件
        /// </summary>
        /// <param name="actorNumber">玩家ActorNumber</param>
        /// <param name="key">属性Key</param>
        public AttributePlayerBase GetAttributePlayerBase(int actorNumber, string key)
        {
            var dictKey = (actorNumber, key);
            if (attributePlayerBaseDictionary.ContainsKey(dictKey))
            {
                return attributePlayerBaseDictionary[dictKey];
            }
            return null;
        }

        /// <summary>
        /// 获取本地玩家的AttributePlayerBase组件（便捷方法）
        /// </summary>
        public AttributePlayerBase GetLocalPlayerAttribute(string key)
        {
            if (PhotonNetwork.LocalPlayer != null)
            {
                return GetAttributePlayerBase(PhotonNetwork.LocalPlayer.ActorNumber, key);
            }
            return null;
        }

        /// <summary>
        /// 注册bool组件
        /// </summary>
        /// <param name="playerName">玩家名称</param>
        /// <param name="boolValue">bool组件值</param>
        public void RegisterBool(string playerName, bool boolValue)
        {
            if (boolDictionary.ContainsKey(playerName))
            {
                boolDictionary[playerName] = boolValue;
            }
            else
            {
                boolDictionary.Add(playerName, boolValue);
            }
        }

        /// <summary>
        /// 注销bool组件
        /// </summary>
        /// <param name="playerName">玩家名称</param>
        public void UnRegisterBool(string playerName)
        {
            if (boolDictionary.ContainsKey(playerName))
            {
                boolDictionary.Remove(playerName);
            }
        }

        /// <summary>
        /// 获取bool组件
        /// </summary>
        /// <param name="playerName">玩家名称</param>
        /// <returns>bool组件值</returns>
        public bool GetBool(string playerName)
        {
            if (boolDictionary.ContainsKey(playerName))
            {
                return boolDictionary[playerName];
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 注册bool参数事件
        /// </summary>
        /// <param name="playerName">玩家名称</param>
        /// <param name="action">bool参数事件</param>
        public void RegisterBoolEvent(string playerName, Action<bool> action)
        {
            if (boolEventDictionary.ContainsKey(playerName))
            {
                boolEventDictionary[playerName] += action;
            }
            else
            {
                boolEventDictionary.Add(playerName, action);
            }
        }

        /// <summary>
        /// 注销bool参数事件
        /// </summary>
        /// <param name="playerName">玩家名称</param>
        public void UnRegisterBoolEvent(string playerName)
        {
            if (boolEventDictionary.ContainsKey(playerName))
            {
                boolEventDictionary.Remove(playerName);
            }
        }

        /// <summary>
        /// 触发bool参数事件
        /// </summary>
        /// <param name="playerName">玩家名称</param>
        /// <param name="boolValue">bool参数值</param>
        public void TriggerBoolEvent(string playerName, bool boolValue)
        {
            if (boolEventDictionary.ContainsKey(playerName))
            {
                boolEventDictionary[playerName]?.Invoke(boolValue);
            }
        }

        /// <summary>
        /// 注册攻击事件
        /// 双浮点数参数事件注册方法
        /// </summary>
        public void RegisterFloatEvent(string eventName, Action<float, float> action)
        {
            if (floatEventDictionary.ContainsKey(eventName))
            {
                floatEventDictionary[eventName] += action;
            }
            else
            {
                floatEventDictionary.Add(eventName, action);
            }
        }

        /// <summary>
        /// 注销双浮点数参数事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        public void UnRegisterFloatEvent(string eventName)
        {
            if (floatEventDictionary.ContainsKey(eventName))
            {
                floatEventDictionary.Remove(eventName);
            }
        }

        /// <summary>
        /// 触发双浮点数参数事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="MaxHP">最大生命值</param>
        /// <param name="CurrentHP">当前生命值</param>
        public void TriggerFloatEvent(string eventName, float MaxHP, float CurrentHP)
        {
            if (floatEventDictionary.ContainsKey(eventName))
            {
                floatEventDictionary[eventName]?.Invoke(MaxHP, CurrentHP);
            }
        }

        //注册CardConfigBase参数字典的方法
        public void RegisterCardEvent(string eventName, Action<CardConfigBase> action)
        {
            if (cardConfigDictionary.ContainsKey(eventName))
            {
                cardConfigDictionary[eventName] += action;
            }
            else
            {
                cardConfigDictionary.Add(eventName, action);
            }
        }

        //注销事件
        public void UnRegisterCardEvent(string eventName)
        {
            if (cardConfigDictionary.ContainsKey(eventName))
            {
                cardConfigDictionary.Remove(eventName);
            }
        }

        //触发
        public void TriggerCardConEvent(string eventName, CardConfigBase cardConfig)
        {
            if (cardConfigDictionary.ContainsKey(eventName))
            {
                cardConfigDictionary[eventName]?.Invoke(cardConfig);
            }
        }
        #endregion
    }

    public class EventNames
    {
        public const string DamageReceived = "DamageReceived";
        public const string UpdateAttributePlayer = "UpdateAttributePlayer";
        public const string RuneInfo = "RuneInfo";
        public const string ChangeCanRotate = "ChangeCanRotate";

        // 玩家属性Key常量
        public const string PlayerAttribute_Main = "MainPlayer";
        public const string PlayerAttribute_Current = "CurrentPlayer";
        public const string HPChanged = "HPChanged";
        public const string DamageReceiverPlayer = "DamageReceiverPlayer";

        //卡牌相关事件
        public const string OnReceiveCard_WizardBoy = "OnReceiveCard_WizardBoy";
    }
}
