using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Controllers.Character;
using Core;
using Core.Channels;
using Core.Channels.General;
using Core.Channels.Player;
using Core.Contracts;
using Core.Network;
using Photon.Pun;
using UnityEngine;
using Managers;

namespace Controllers.Player
{
    public class PlayerManager : MonoBehaviour
    {
        /// <summary>玩家死亡时触发，参数为死亡的 ActorNumber（字符串形式）</summary>
        public static event Action<string> OnPlayerDeath;

        /// <summary>
        /// 属性缓存服务（统一管理 AttributePlayerBase 的增删查）
        /// </summary>
        private AttributeCacheService attributeCache;

        #region 单例模式
        

        void Awake()
        {
            ServiceLocator.Register(this);
            DontDestroyOnLoad(gameObject);
            attributeCache = new AttributeCacheService();
        }

        void OnEnable()
        {
            EventChannelLocator.MainContainer.gameActionChannel.RegisterListener(OnGameAction);
            EventChannelLocator.MainContainer.playerQueryChannel.RegisterListener(OnPlayerQuery);
            if (NetworkServiceLocator.IsInitialized)
            {
                NetworkServiceLocator.PlayerService.OnPlayerEnteredRoom += OnPlayerEnteredRoom;
                NetworkServiceLocator.PlayerService.OnPlayerLeftRoom += OnPlayerLeftRoom;
                NetworkServiceLocator.PlayerService.OnLocalJoinedRoom += OnLocalJoinedRoom;
            }
        }

        void OnDisable()
        {
            EventChannelLocator.MainContainer.gameActionChannel.UnregisterListener(OnGameAction);
            EventChannelLocator.MainContainer.playerQueryChannel.UnregisterListener(OnPlayerQuery);
            if (NetworkServiceLocator.IsInitialized)
            {
                NetworkServiceLocator.PlayerService.OnPlayerEnteredRoom -= OnPlayerEnteredRoom;
                NetworkServiceLocator.PlayerService.OnPlayerLeftRoom -= OnPlayerLeftRoom;
                NetworkServiceLocator.PlayerService.OnLocalJoinedRoom -= OnLocalJoinedRoom;
            }
            attributeCache?.Dispose();
        }

        void OnDestroy()
        {
            ServiceLocator.Unregister<PlayerManager>();
        }

        private void OnGameAction(GameActionType actionType)
        {
            if (actionType == GameActionType.SyncAllPlayers)
                SyncAllPlayers();
        }

        private void OnPlayerQuery(PlayerQueryData data)
        {
            if (data == null) return;
            switch (data.queryType)
            {
                case PlayerQueryType.RegisterPlayerObject:
                    RegisterPlayerObject(data.playerObject);
                    break;
                case PlayerQueryType.UnregisterPlayerObject:
                    UnregisterPlayerObject(data.playerObject);
                    break;
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

        private void OnPlayerEnteredRoom(int actorNumber, string userId)
        {
            if (NetworkServiceLocator.PlayerService.IsConnectedAndInRoom)
            {
                string nickName = NetworkServiceLocator.PlayerService.GetPlayerNickName(actorNumber)
                                  ?? "Player_" + actorNumber;
                AddPlayer(new PlayerData(userId, nickName));
            }
        }

        // 当本地玩家加入房间时
        private void OnLocalJoinedRoom()
        {
            Debug.Log("[PlayerManager] 本地玩家加入房间，同步所有玩家");
            SyncAllPlayers();
        }

        // 当玩家离开房间时
        private void OnPlayerLeftRoom(int actorNumber, string userId)
        {
            if (NetworkServiceLocator.PlayerService.IsConnectedAndInRoom)
            {
                RemovePlayer(userId);
            }
        }

        // 同步所有玩家数据
        public void SyncAllPlayers()
        {
            var playerService = NetworkServiceLocator.PlayerService;
            if (!playerService.IsConnectedAndInRoom)
            {
                Debug.LogWarning("[PlayerManager] 未在房间中，无法同步玩家");
                return;
            }

            playerDataDict.Clear();

            // 遍历房间内所有玩家，逐个添加（包括本地玩家和其他玩家）
            int[] actorNumbers = playerService.GetAllActorNumbers();
            foreach (int actorNumber in actorNumbers)
            {
                string userId = playerService.GetPlayerUserId(actorNumber);
                if (string.IsNullOrEmpty(userId))
                    continue;

                string nickName = playerService.GetPlayerNickName(actorNumber)
                                  ?? "Player_" + actorNumber;

                var playerData = new PlayerData(userId, nickName);
                AddPlayer(playerData);
            }

            Debug.Log($"[PlayerManager] 同步完成，当前玩家数量: {PlayerCount}");
            PrintAllPlayers();
        }

        // 获取本地玩家数据
        public PlayerData GetLocalPlayer()
        {
            string localUserId = NetworkServiceLocator.PlayerService.GetLocalUserId();
            if (!string.IsNullOrEmpty(localUserId))
            {
                return GetPlayer(localUserId);
            }
            return null;
        }

        // 检查是否是本地玩家
        public bool IsLocalPlayer(string playerId)
        {
            string localUserId = NetworkServiceLocator.PlayerService.GetLocalUserId();
            return !string.IsNullOrEmpty(localUserId) && localUserId == playerId;
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

        // ==================== 存活状态追踪 ====================

        private HashSet<string> deadPlayerIds = new HashSet<string>();

        /// <summary>标记玩家死亡</summary>
        public void SetPlayerDead(string playerId)
        {
            if (deadPlayerIds.Add(playerId))
            {
                OnPlayerDeath?.Invoke(playerId);
            }
        }

        /// <summary>标记玩家复活（预留，当前不使用）</summary>
        public void SetPlayerAlive(string playerId)
        {
            deadPlayerIds.Remove(playerId);
        }

        /// <summary>玩家是否存活</summary>
        public bool IsPlayerAlive(string playerId)
        {
            return !deadPlayerIds.Contains(playerId);
        }

        /// <summary>
        /// 获取所有存活队友的 Transform（排除死亡玩家和本地玩家自己）
        /// </summary>
        /// <param name="localActorNumber">本地玩家 ActorNumber</param>
        public List<Transform> GetAliveTeammateTransforms(int localActorNumber)
        {
            var result = new List<Transform>();
            foreach (var go in activePlayerObjects)
            {
                if (go == null) continue;

                var pv = go.GetComponentInParent<PhotonView>();
                if (pv == null) continue;

                int ownerActor = pv.Owner != null ? pv.Owner.ActorNumber : -1;
                if (ownerActor == localActorNumber) continue;
                if (deadPlayerIds.Contains(ownerActor.ToString())) continue;

                result.Add(go.transform);
            }
            return result;
        }

        /// <summary>
        /// 清除所有死亡状态（场景切换/重开时调用）
        /// </summary>
        public void ClearAllDeathState()
        {
            deadPlayerIds.Clear();
        }

        public AttributePlayerBase GetLocalPlayerAttribute(string key)
        {
            return attributeCache?.GetLocalAttribute(key);
        }
    }
}
