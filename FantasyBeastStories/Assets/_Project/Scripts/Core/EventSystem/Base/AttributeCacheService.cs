using System.Collections.Generic;
using Controllers.Character;
using Core.Channels.Player;
using Core.Contracts;
using Core.Network;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// 玩家属性缓存服务（纯 C# 类）
    /// 监听 PlayerAttributeEventChannelSO 维护 (ActorNumber, AttributeKey) → AttributePlayerBase 的字典
    /// 替代原来的 EventManager（MonoBehaviour + 废弃单例）
    /// </summary>
    public class AttributeCacheService
    {
        /// <summary>
        /// 属性缓存字典：(actorNumber, attributeKey) → AttributePlayerBase
        /// </summary>
        private readonly Dictionary<(int actorNumber, string key), AttributePlayerBase> _cache =
            new Dictionary<(int, string), AttributePlayerBase>();

        public AttributeCacheService()
        {
            var channel = EventChannelLocator.MainContainer.playerAttributeChannel;
            if (channel != null)
            {
                channel.RegisterListener(OnPlayerAttributeQuery);
            }
            else
            {
                Debug.LogError("[AttributeCacheService] playerAttributeChannel 为空，无法监听属性查询");
            }
        }

        /// <summary>
        /// 销毁时取消注册
        /// </summary>
        public void Dispose()
        {
            var channel = EventChannelLocator.MainContainer.playerAttributeChannel;
            if (channel != null)
            {
                channel.UnregisterListener(OnPlayerAttributeQuery);
            }
        }

        /// <summary>
        /// 接收 PlayerAttributeEventChannelSO 的回调
        /// 处理 RegisterAttribute / UnregisterAttribute / GetAttributeById / GetLocalPlayerAttribute
        /// </summary>
        private void OnPlayerAttributeQuery(PlayerAttributeData data)
        {
            switch (data.queryType)
            {
                case PlayerAttributeQueryType.RegisterAttribute:
                    if (int.TryParse(data.playerId, out int actorNum))
                    {
                        var key = (actorNum, data.attributeName);
                        _cache[key] = data.attribute;
                    }
                    break;

                case PlayerAttributeQueryType.UnregisterAttribute:
                    if (int.TryParse(data.playerId, out int unregActorNum))
                    {
                        var unregKey = (unregActorNum, data.attributeName);
                        _cache.Remove(unregKey);
                    }
                    break;

                case PlayerAttributeQueryType.GetAttributeById:
                    if (int.TryParse(data.playerId, out int getActorNum))
                    {
                        var getKey = (getActorNum, data.attributeName);
                        _cache.TryGetValue(getKey, out data.attribute);
                    }
                    break;

                case PlayerAttributeQueryType.GetLocalPlayerAttribute:
                    int localActorNum = NetworkServiceLocator.PlayerService.GetLocalActorNumber();
                    var localKey = (localActorNum, data.attributeName);
                    _cache.TryGetValue(localKey, out data.attribute);
                    break;
            }
        }

        /// <summary>
        /// 外部查询属性
        /// </summary>
        public AttributePlayerBase GetAttribute(int actorNumber, string key)
        {
            var dictKey = (actorNumber, key);
            if (_cache.TryGetValue(dictKey, out var value))
                return value;
            return null;
        }

        /// <summary>
        /// 获取本地玩家属性
        /// </summary>
        public AttributePlayerBase GetLocalAttribute(string key)
        {
            int localActorNum = NetworkServiceLocator.PlayerService.GetLocalActorNumber();
            if (localActorNum > 0)
                return GetAttribute(localActorNum, key);
            return null;
        }
    }
}