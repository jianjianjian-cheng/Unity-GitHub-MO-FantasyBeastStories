using Domain.Character.Attribute;
using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.Player
{
    public enum PlayerAttributeQueryType
    {
        GetLocalPlayerAttribute,
        RegisterAttribute,
        UnregisterAttribute,
        GetAttributeById
    }

    [CreateAssetMenu(menuName = "Events/Player/Player Attribute Event Channel")]
    public class PlayerAttributeEventChannelSO : BaseEventChannelSO<PlayerAttributeData>
    {
    }

    public class PlayerAttributeData : EventArgsBase
    {
        public PlayerAttributeQueryType queryType;
        public string playerId;
        public AttributePlayerBase attribute;
        public string attributeName;

        public PlayerAttributeData(PlayerAttributeQueryType queryType)
        {
            this.queryType = queryType;
        }

        public PlayerAttributeData(PlayerAttributeQueryType queryType, string attributeName)
        {
            this.queryType = queryType;
            this.attributeName = attributeName;
        }

        public PlayerAttributeData(PlayerAttributeQueryType queryType, string playerId, AttributePlayerBase attribute)
        {
            this.queryType = queryType;
            this.playerId = playerId;
            this.attribute = attribute;
        }
    }
}