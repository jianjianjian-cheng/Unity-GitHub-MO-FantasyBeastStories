using System.Collections.Generic;
using Controllers.Services;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Controllers.Network
{
    public class PhotonPlayerService : INetworkPlayerService
    {
        private readonly Dictionary<int, Photon.Realtime.Player> _playerCache = new();

        public event System.Action<int, string, object> OnPlayerPropertyChanged;
        public event System.Action<int, string> OnPlayerEnteredRoom;
        public event System.Action<int, string> OnPlayerLeftRoom;
        public event System.Action OnLocalJoinedRoom;

        public bool IsConnectedAndInRoom => PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom;
        public bool IsMasterClient => PhotonNetwork.IsMasterClient;

        public int GetLocalActorNumber()
        {
            if (PhotonNetwork.LocalPlayer != null)
                return PhotonNetwork.LocalPlayer.ActorNumber;
            return -1;
        }

        public string GetLocalUserId()
        {
            if (PhotonNetwork.LocalPlayer != null)
                return PhotonNetwork.LocalPlayer.UserId;
            return string.Empty;
        }

        public bool IsOwnerOf(GameObject gameObject)
        {
            if (gameObject == null) return false;
            PhotonView pv = gameObject.GetComponentInParent<PhotonView>();
            return pv != null && pv.IsMine;
        }

        public void SetCustomProperty(string key, object value)
        {
            if (!IsConnectedAndInRoom) return;
            var props = new ExitGames.Client.Photon.Hashtable { { key, value } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        public object GetCustomProperty(string key)
        {
            if (!IsConnectedAndInRoom || PhotonNetwork.LocalPlayer == null)
                return null;
            return PhotonNetwork.LocalPlayer.CustomProperties[key];
        }

        public int[] GetAllActorNumbers()
        {
            if (!IsConnectedAndInRoom) return System.Array.Empty<int>();
            var players = PhotonNetwork.PlayerList;
            int[] actorNumbers = new int[players.Length];
            for (int i = 0; i < players.Length; i++)
            {
                actorNumbers[i] = players[i].ActorNumber;
            }
            return actorNumbers;
        }

        public object GetPlayerCustomProperty(int actorNumber, string key)
        {
            if (!IsConnectedAndInRoom) return null;
            var player = FindPlayer(actorNumber);
            return player != null ? player.CustomProperties[key] : null;
        }

        public string GetPlayerUserId(int actorNumber)
        {
            if (!IsConnectedAndInRoom) return null;
            var player = FindPlayer(actorNumber);
            return player != null ? player.UserId : null;
        }

        public string GetPlayerNickName(int actorNumber)
        {
            if (!IsConnectedAndInRoom) return null;
            var player = FindPlayer(actorNumber);
            return player != null ? player.NickName : null;
        }

        public bool AllPlayersHaveProperty(string key, object value)
        {
            if (!IsConnectedAndInRoom) return false;
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (!player.CustomProperties.ContainsKey(key))
                    return false;
                if (!player.CustomProperties[key].Equals(value))
                    return false;
            }
            return true;
        }

        public void SetRoomCustomProperty(string key, object value)
        {
            if (!IsConnectedAndInRoom || PhotonNetwork.CurrentRoom == null) return;
            var props = new ExitGames.Client.Photon.Hashtable { { key, value } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        public object GetRoomCustomProperty(string key)
        {
            if (!IsConnectedAndInRoom || PhotonNetwork.CurrentRoom == null)
                return null;
            return PhotonNetwork.CurrentRoom.CustomProperties[key];
        }

        /// <summary>
        /// 由 PhotonCallbackBridge 调用，转发 PUN 属性变更事件
        /// </summary>
        public void NotifyPropertyChanged(int actorNumber, string key, object value)
        {
            OnPlayerPropertyChanged?.Invoke(actorNumber, key, value);
        }

        /// <summary>
        /// 由 PhotonCallbackBridge 调用，转发玩家加入事件
        /// </summary>
        public void NotifyPlayerEnteredRoom(int actorNumber, string userId)
        {
            var player = FindPlayer(actorNumber);
            if (player != null)
                _playerCache[actorNumber] = player;
            OnPlayerEnteredRoom?.Invoke(actorNumber, userId);
        }

        /// <summary>
        /// 由 PhotonCallbackBridge 调用，转发玩家离开事件
        /// </summary>
        public void NotifyPlayerLeftRoom(int actorNumber, string userId)
        {
            _playerCache.Remove(actorNumber);
            OnPlayerLeftRoom?.Invoke(actorNumber, userId);
        }

        /// <summary>
        /// 由 PhotonCallbackBridge 调用，转发本地玩家加入完成事件
        /// </summary>
        public void NotifyLocalJoinedRoom()
        {
            _playerCache.Clear();
            foreach (var player in PhotonNetwork.PlayerList)
                _playerCache[player.ActorNumber] = player;
            OnLocalJoinedRoom?.Invoke();
        }

        private Photon.Realtime.Player FindPlayer(int actorNumber)
        {
            if (_playerCache.TryGetValue(actorNumber, out var cached))
                return cached;
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player.ActorNumber == actorNumber)
                {
                    _playerCache[actorNumber] = player;
                    return player;
                }
            }
            return null;
        }
    }
}