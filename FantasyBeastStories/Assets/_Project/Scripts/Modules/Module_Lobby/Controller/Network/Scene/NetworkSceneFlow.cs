using System.Collections;
using UnityEngine;
using ExitGames.Client.Photon;
using Photon.Pun;
using Core.Contracts;
using Core.Network;
using UI.Framework.Panel;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Managers;

namespace Controllers.Network
{
    /// <summary>
    /// 准备系统、场景加载协调、返回大厅逻辑（从 Launcher 拆分）
    /// 非 MonoBehaviour 纯逻辑类，由 Launcher 持有和驱动协程
    /// </summary>
    public class NetworkSceneFlow
    {
        private readonly MonoBehaviour _coroutineRunner;

        private bool isRoomLoading = false;
        private bool isLoadingScene = false;
        private bool allPlayersLoaded = false;

        public NetworkSceneFlow(MonoBehaviour coroutineRunner)
        {
            _coroutineRunner = coroutineRunner;
        }

        // ==================== 准备系统 ====================

        /// <summary>设置本地玩家的准备状态</summary>
        public void SetLocalReady(bool ready)
        {
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[NetworkSceneFlow] 无法设置准备状态：未在房间中");
                return;
            }

            Hashtable props = new Hashtable { { PlayerPropertyKeys.Ready, ready } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            Debug.Log($"[NetworkSceneFlow] 本地玩家准备状态: {ready} - {PhotonNetwork.LocalPlayer.NickName}");

            // 显式通知本地玩家的属性变更，确保本地 WorldSpaceUI 头顶文字能收到更新
            // （PUN2 的 OnPlayerPropertiesUpdate 不保证对本地客户端回调本地玩家的属性变更）
            var playerService = NetworkServiceLocator.PlayerService as PhotonPlayerService;
            if (playerService != null)
            {
                playerService.NotifyPropertyChanged(
                    PhotonNetwork.LocalPlayer.ActorNumber,
                    PlayerPropertyKeys.Ready,
                    ready
                );
            }

            CheckAllPlayersReady();
        }

        private bool AllPlayersReady()
        {
            foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
            {
                if (
                    !player.CustomProperties.ContainsKey(PlayerPropertyKeys.Ready)
                    || (bool)player.CustomProperties[PlayerPropertyKeys.Ready] == false
                )
                {
                    return false;
                }
            }
            return true;
        }

        public void CheckAllPlayersReady()
        {
            if (isRoomLoading || isLoadingScene)
            {
                return;
            }

            if (AllPlayersReady())
            {
                Debug.Log("[NetworkSceneFlow] 所有玩家已准备，开始加载场景");
                isRoomLoading = true;
                _coroutineRunner.StartCoroutine(LoadLevelAfterDelay());
            }
        }

        // ==================== 场景加载 ====================

        public IEnumerator LoadLevelAfterDelay(int index = 2)
        {
            isLoadingScene = true;
            yield return new WaitForSeconds(2f);

            yield return ServiceLocator.Get<Loading>().Show();

            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(index);
            }
        }

        /// <summary>检查所有玩家是否已加载场景（由 Launcher.OnPlayerPropertiesUpdate 委托）</summary>
        public void CheckAllPlayersLoaded()
        {
            if (isLoadingScene || allPlayersLoaded)
            {
                return;
            }

            foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
            {
                if (
                    !player.CustomProperties.ContainsKey(PlayerPropertyKeys.Loaded)
                    || (bool)player.CustomProperties[PlayerPropertyKeys.Loaded] == false
                )
                {
                    Debug.Log($"[NetworkSceneFlow] 等待玩家 {player.NickName} 加载场景...");
                    return;
                }
            }

            allPlayersLoaded = true;
            Debug.Log("[NetworkSceneFlow] 所有玩家已加载完成，开始游戏！");
        }

        // ==================== 返回大厅 ====================

        /// <summary>返回大厅（保持房间连接）</summary>
        public void ReturnToLobby()
        {
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[NetworkSceneFlow] 不在房间中，无法返回大厅");
                return;
            }

            _coroutineRunner.StartCoroutine(ReturnToLobbyCoroutine());
        }

        private IEnumerator ReturnToLobbyCoroutine()
        {
            yield return ServiceLocator.Get<Loading>().Show();

            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(1);
                Debug.Log("[NetworkSceneFlow] 房主发起切换到大厅场景");
            }
            else
            {
                Debug.Log("[NetworkSceneFlow] 等待房主同步场景...");
            }
        }

        // ==================== 状态重置（场景切换时由 Launcher 调用） ====================

        /// <summary>重置所有流程标志（大厅和游戏场景切换时均由 Launcher 调用）</summary>
        public void ResetForLobby()
        {
            isRoomLoading = false;
            isLoadingScene = false;
            allPlayersLoaded = false;
        }

        /// <summary>重置本地玩家的准备状态</summary>
        public void ResetLocalReady()
        {
            if (
                PhotonNetwork.IsConnected
                && PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PlayerPropertyKeys.Ready)
            )
            {
                Hashtable props = new Hashtable { { PlayerPropertyKeys.Ready, false } };
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
                Debug.Log("[NetworkSceneFlow] 重置本地玩家就绪状态为 false");
            }
        }
    }
}
