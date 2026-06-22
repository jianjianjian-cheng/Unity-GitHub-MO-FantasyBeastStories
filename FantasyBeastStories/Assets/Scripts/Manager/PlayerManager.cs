using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

namespace Manager
{
    public class PlayerManager : MonoBehaviourPunCallbacks
    {

        #region 单例模式
        public static PlayerManager instance;

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
        // 存储所有玩家数据
        private Dictionary<string, PlayerData> playerDataDict = new Dictionary<string, PlayerData>();

        // 缓存所有玩家GameObject，供敌人追踪使用，避免每帧 FindGameObjectsWithTag
        private List<GameObject> activePlayerObjects = new List<GameObject>();
        public IReadOnlyList<GameObject> ActivePlayerObjects => activePlayerObjects;

        // 公开的只读访问
        public IReadOnlyDictionary<string, PlayerData> AllPlayers => playerDataDict;
        public List<PlayerData> PlayerList => playerDataDict.Values.ToList();



        // 获取玩家数量
        public int PlayerCount => playerDataDict.Count;
        /// <summary>
        /// 添加玩家数据到字典中
        /// </summary>
        /// <param name="playerData">玩家数据</param>
        public void AddPlayer(PlayerData playerData)
        {
            // 双重保险
            if (playerData == null || string.IsNullOrEmpty(playerData.PlayerId))
            {
                Debug.LogError("[PlayerManager] AddPlayer: playerData 无效");
                return;
            }

            if (!playerDataDict.ContainsKey(playerData.PlayerId))
            {
                playerDataDict.Add(playerData.PlayerId, playerData);
                Debug.Log($"添加玩家 {playerData.PlayerName} 到字典");
            }
        }

        /// <summary>
        /// 从字典中移除玩家数据
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        public void RemovePlayer(string playerId)
        {
            if (playerDataDict.ContainsKey(playerId))
            {
                playerDataDict.Remove(playerId);
                Debug.Log($"移除玩家 {playerId} 从字典");
            }
        }

        /// <summary>
        /// 获取玩家数据
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>玩家数据</returns>
        public PlayerData GetPlayer(string playerId)
        {
            if (playerDataDict.ContainsKey(playerId))
            {
                return playerDataDict[playerId];
            }
            else
            {
                Debug.LogError($"玩家 {playerId} 不存在");
                return null;
            }
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);
            if (PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null)
            {
                AddPlayer(PlayerData.FromPhotonPlayer(newPlayer));
            }
        }

        // 当本地玩家加入房间时
        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            Debug.Log("[PlayerManager] 本地玩家加入房间，同步所有玩家");
            SyncAllPlayers();
        }

        // 当玩家离开房间时
        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            base.OnPlayerLeftRoom(otherPlayer);

            if (PhotonNetwork.InRoom)
            {
                RemovePlayer(otherPlayer.UserId);
            }
        }

        // 同步所有玩家数据
        public void SyncAllPlayers()
        {
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[PlayerManager] 未在房间中，无法同步玩家");
                return;
            }

            playerDataDict.Clear();

            // 首先添加本地玩家
            if (PhotonNetwork.LocalPlayer != null)
            {
                PlayerData localData = PlayerData.FromPhotonPlayer(PhotonNetwork.LocalPlayer);
                AddPlayer(localData);
            }

            // 添加其他玩家
            foreach (var player in PhotonNetwork.PlayerListOthers)
            {
                PlayerData playerData = PlayerData.FromPhotonPlayer(player);
                AddPlayer(playerData);
            }

            Debug.Log($"[PlayerManager] 同步完成，当前玩家数量: {PlayerCount}");
            PrintAllPlayers();
        }

        // 获取本地玩家数据
        public PlayerData GetLocalPlayer()
        {
            if (PhotonNetwork.LocalPlayer != null)
            {
                return GetPlayer(PhotonNetwork.LocalPlayer.UserId);
            }
            return null;
        }

        // 检查是否是本地玩家
        public bool IsLocalPlayer(string playerId)
        {
            return PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.UserId == playerId;
        }

        // 打印所有玩家信息
        public void PrintAllPlayers()
        {
            if (playerDataDict.Count == 0)
            {
                Debug.Log("[PlayerManager] 当前没有玩家");
                return;
            }

            Debug.Log($"========== 玩家列表 (共 {PlayerCount} 人) ==========");

            int index = 1;
            foreach (var kvp in playerDataDict)
            {
                string isLocal = IsLocalPlayer(kvp.Key) ? " (本地)" : "";
                Debug.Log($"{index}. 昵称: {kvp.Value.PlayerName}\tID: {kvp.Key}{isLocal}");
                index++;
            }

            Debug.Log("============================================");
        }


        /// <summary>
        /// 注册玩家GameObject（玩家OnEnable时调用）
        /// </summary>
        public void RegisterPlayerObject(GameObject playerObj)
        {
            if (playerObj != null && !activePlayerObjects.Contains(playerObj))
                activePlayerObjects.Add(playerObj);
        }

        /// <summary>
        /// 注销玩家GameObject（玩家OnDisable时调用）
        /// </summary>
        public void UnregisterPlayerObject(GameObject playerObj)
        {
            if (playerObj != null)
                activePlayerObjects.Remove(playerObj);
        }

        //返回除本地玩家外的其他玩家的ID列表
        public List<string> GetOtherPlayersIds()
        {
            return playerDataDict.Keys.Where(p => !IsLocalPlayer(p)).ToList();
        }


    }
}
