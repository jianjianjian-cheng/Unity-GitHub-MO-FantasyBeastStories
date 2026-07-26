using System.Collections.Generic;
using Core.Contracts;
using Core.Network;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Controllers.Network
{
    /// <summary>
    /// PUN 回调桥接器 — 将 PUN 的 MonoBehaviourPunCallbacks 事件转发给 NetworkServiceLocator
    /// </summary>
    public class PhotonCallbackBridge : MonoBehaviourPunCallbacks
    {
        private static PhotonCallbackBridge _instance;
        private static readonly List<PhotonView> _pendingBridgeViews = new();

        public static void EnsureExists()
        {
            if (_instance == null)
            {
                var go = new GameObject("PhotonCallbackBridge");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<PhotonCallbackBridge>();
            }
        }

        /// <summary>注册 RPC Bridge 的 PhotonView，在 OnJoinedRoom 时统一分配 ViewID</summary>
        public static void RegisterBridgeView(PhotonView pv)
        {
            if (pv != null && !_pendingBridgeViews.Contains(pv))
                _pendingBridgeViews.Add(pv);
        }

        private PhotonPlayerService PlayerService
        {
            get
            {
                var service = NetworkServiceLocator.PlayerService as PhotonPlayerService;
                if (service == null)
                    Debug.LogWarning("[PhotonCallbackBridge] PlayerService 未注册或不是 PhotonPlayerService");
                return service;
            }
        }

        public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            var service = PlayerService;
            if (service == null) return;
            foreach (var key in changedProps.Keys)
            {
                service.NotifyPropertyChanged(targetPlayer.ActorNumber, key.ToString(), changedProps[key]);
            }
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            var service = PlayerService;
            if (service == null) return;
            service.NotifyPlayerEnteredRoom(newPlayer.ActorNumber, newPlayer.UserId);
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
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

            // 加入房间后，为三个 RPC Bridge 分配 PhotonView View ID
            // 它们在 BeforeSceneLoad 时用 new GameObject() 创建，当时 Photon 未连接，View ID 为 0
            AllocateBridgeViewIDs();
        }

        /// <summary>
        /// 为已注册的 RPC Bridge PhotonView 分配 View ID
        /// </summary>
        private static void AllocateBridgeViewIDs()
        {
            foreach (var pv in _pendingBridgeViews)
            {
                if (pv == null) continue;
                if (pv.ViewID == 0)
                {
                    if (PhotonNetwork.AllocateViewID(pv))
                        Debug.Log($"[PhotonCallbackBridge] 已为 {pv.gameObject.name} 分配 View ID: {pv.ViewID}");
                    else
                        Debug.LogWarning($"[PhotonCallbackBridge] 为 {pv.gameObject.name} 分配 View ID 失败");
                }
            }
        }
    }
}