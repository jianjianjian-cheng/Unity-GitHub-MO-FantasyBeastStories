using UnityEngine;

namespace Domain.Services
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
        bool AllPlayersHaveProperty(string key, object value);
        void SetRoomCustomProperty(string key, object value);
        object GetRoomCustomProperty(string key);
        event System.Action<int, string, object> OnPlayerPropertyChanged;
        event System.Action<int, string> OnPlayerEnteredRoom;
        event System.Action<int, string> OnPlayerLeftRoom;
        event System.Action OnLocalJoinedRoom;
    }
}