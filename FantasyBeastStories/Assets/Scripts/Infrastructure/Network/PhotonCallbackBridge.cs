using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Infrastructure.Network
{
    /// <summary>
    /// 轻量级 PUN 回调桥接器 — 将 PUN 的 MonoBehaviourPunCallbacks 事件转发给 NetworkServiceLocator
    /// </summary>
    public class PhotonCallbackBridge : MonoBehaviourPunCallbacks
    {
        private static PhotonCallbackBridge _instance;

        public static void EnsureExists()
        {
            if (_instance == null)
            {
                var go = new GameObject("PhotonCallbackBridge");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<PhotonCallbackBridge>();
            }
        }

        private PhotonPlayerService PlayerService
        {
            get
            {
                var service = Domain.Services.NetworkServiceLocator.PlayerService as PhotonPlayerService;
                if (service == null)
                    Debug.LogWarning("[PhotonCallbackBridge] PlayerService 未注册或不是 PhotonPlayerService");
                return service;
            }
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            var service = PlayerService;
            if (service == null) return;
            foreach (var key in changedProps.Keys)
            {
                service.NotifyPropertyChanged(targetPlayer.ActorNumber, key.ToString(), changedProps[key]);
            }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            var service = PlayerService;
            if (service == null) return;
            service.NotifyPlayerEnteredRoom(newPlayer.ActorNumber, newPlayer.UserId);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            var service = PlayerService;
            if (service == null) return;
            service.NotifyPlayerLeftRoom(otherPlayer.ActorNumber, otherPlayer.UserId);
        }

        public override void OnJoinedRoom()
        {
            var service = PlayerService;
            if (service == null) return;
            service.NotifyLocalJoinedRoom();
        }
    }
}