using Core.Channels.Base;
using UnityEngine;

namespace Core.Channels.Player
{
    public enum PlayerQueryType
    {
        GetPlayerCount,
        GetPlayerList,
        GetActivePlayerObjects,
        GetOtherPlayersIds,
        RegisterPlayerObject,
        UnregisterPlayerObject
    }

    [CreateAssetMenu(menuName = "Events/Player/Player Query Event Channel")]
    public class PlayerQueryEventChannelSO : BaseEventChannelSO<PlayerQueryData>
    {
    }

    public class PlayerQueryData : EventArgsBase
    {
        public PlayerQueryType queryType;
        public GameObject playerObject;
        public string playerId;
        public int intResult;
        public System.Collections.Generic.List<string> stringListResult;
        public System.Collections.Generic.List<GameObject> gameObjectListResult;

        public PlayerQueryData(PlayerQueryType queryType)
        {
            this.queryType = queryType;
        }

        public PlayerQueryData(PlayerQueryType queryType, GameObject playerObject)
        {
            this.queryType = queryType;
            this.playerObject = playerObject;
        }

        public PlayerQueryData(PlayerQueryType queryType, string playerId)
        {
            this.queryType = queryType;
            this.playerId = playerId;
        }
    }
}
