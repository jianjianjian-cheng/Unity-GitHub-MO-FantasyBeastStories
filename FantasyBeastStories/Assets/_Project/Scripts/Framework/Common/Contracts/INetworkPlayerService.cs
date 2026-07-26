using UnityEngine;

namespace Core.Contracts
{
    public interface INetworkPlayerService
    {
        int GetLocalActorNumber();
        string GetLocalUserId();
        bool IsConnectedAndInRoom { get; }
        bool IsMasterClient { get; }
        bool IsOwnerOf(GameObject gameObject);
        void SetCustomProperty(string key, object value);
        object GetCustomProperty(string key);
        object GetPlayerCustomProperty(int actorNumber, string key);
        string GetPlayerUserId(int actorNumber);
        /// <summary>
        /// 获取指定玩家的昵称（直接使用 Photon Player.NickName，不依赖 CustomProperties）
        /// </summary>
        string GetPlayerNickName(int actorNumber);
        bool AllPlayersHaveProperty(string key, object value);
        void SetRoomCustomProperty(string key, object value);
        object GetRoomCustomProperty(string key);
        /// <summary>
        /// 获取房间内所有玩家的 ActorNumber 列表
        /// </summary>
        int[] GetAllActorNumbers();
        event System.Action<int, string, object> OnPlayerPropertyChanged;
        event System.Action<int, string> OnPlayerEnteredRoom;
        event System.Action<int, string> OnPlayerLeftRoom;
        event System.Action OnLocalJoinedRoom;
    }
}