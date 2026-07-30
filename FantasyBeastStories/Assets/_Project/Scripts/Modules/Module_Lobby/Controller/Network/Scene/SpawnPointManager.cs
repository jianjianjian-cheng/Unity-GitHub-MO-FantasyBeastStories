using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using Photon.Pun;
using Core;
using Core.Contracts;
using Core.Network;
using Controllers.Player;
using Controllers.Character;
using UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Core.Save;
using Controllers.Game;

namespace Controllers.Network
{
    /// <summary>
    /// 玩家生成、生成点管理、角色切换逻辑（从 Launcher 拆分）
    /// 非 MonoBehaviour 纯逻辑类，由 Launcher 持有和驱动
    /// </summary>
    public class SpawnPointManager
    {
        private readonly Launcher _launcher;
        private readonly GameObject _currentlySelectedCharacter;
        private readonly CharacterInfoLibrarySO _characterInfoLibrary;

        // Phase 3: 当前选中的角色名（用于 PlayerCharacter 预制体 + instantiationData）
        private string _selectedCharacterName;

        private InputField _nameUI;
        private Photon.Realtime.Player _localPlayer;
        private GameObject _localPlayerObject;

        public GameObject LocalPlayerObject => _localPlayerObject;
        public Photon.Realtime.Player LocalPlayer => _localPlayer;

        public SpawnPointManager(Launcher launcher, GameObject currentlySelectedCharacter,
            CharacterInfoLibrarySO characterInfoLibrary = null)
        {
            _launcher = launcher;
            _currentlySelectedCharacter = currentlySelectedCharacter;
            _characterInfoLibrary = characterInfoLibrary;
        }

        /// <summary>大厅 UI 初始化完成后延迟注入 nameUI</summary>
        public void SetNameUI(InputField nameUI) => _nameUI = nameUI;

        // ==================== 玩家生成入口 ====================

        /// <summary>玩家加入房间 / 场景加载完成后的统一生成入口</summary>
        public void CreatedOrJoinedRoom()
        {
            if (SceneManager.GetActiveScene().buildIndex < 1)
            {
                Debug.Log("[SpawnPointManager] 当前不是游戏场景，跳过玩家生成");
                return;
            }
            ServiceLocator.Get<GameManager>().FindSpawnPoints();
            Debug.Log("[SpawnPointManager] 执行 CreatedOrJoinedRoom");

            EnsurePlayerManagerExists();

            ApplySavedCharacter();

            GameObject player = SpawnPlayer();

            if (player != null)
            {
                InitializePlayerSettings(player);
                SetupPlayerUI(player);
            }

            SetupRoomInfo();
        }

        /// <summary>大厅场景延迟生成角色</summary>
        public IEnumerator SpawnPlayerAfterDelay()
        {
            yield return new WaitForEndOfFrame();

            yield return new WaitUntil(() =>
                ServiceLocator.Get<GameManager>() != null && ServiceLocator.Get<GameManager>().GetEmptySpawnPoint() != null
            );

            ApplySavedCharacter();

            GameObject player = SpawnPlayer();
            if (player != null)
            {
                InitializePlayerSettings(player);
                SetupPlayerUI(player);
                Debug.Log("[SpawnPointManager] 在大厅重新生成角色");
            }
        }

        // ==================== 核心：生成玩家 ====================

        public GameObject SpawnPlayer()
        {
            int localActorNumber = NetworkServiceLocator.PlayerService.GetLocalActorNumber();
            var gameManager = ServiceLocator.Get<GameManager>();

            // 根据 ActorNumber 确定性地分配生成点（不同玩家必然得到不同生成点）
            ISpawnPoint sp = gameManager.GetSpawnPointForPlayer(localActorNumber);
            Transform spawnPoint = (sp as MonoBehaviour)?.transform;

            // 仅当确定性分配的生成点被"其他"玩家占用时才回退（RPC 同步滞后期间 IsEmpty 可能误报）
            if (sp != null && !sp.IsEmpty() && sp.GetOccupiedByPlayer() != localActorNumber)
            {
                sp = gameManager.GetEmptySpawnPoint()?.GetComponent<ISpawnPoint>();
                spawnPoint = (sp as MonoBehaviour)?.transform;
            }

            if (spawnPoint == null)
            {
                Debug.LogError("[SpawnPointManager] 没有可用的生成点！");
                return null;
            }

            if (sp != null)
            {
                sp.SetOccupied(true, localActorNumber);
            }

            Vector3 spawnPosition = CalculateSpawnPosition(spawnPoint);

            // Phase 3: 使用角色名作为预制体名，角色名通过 instantiationData 传递
            string prefabName = string.IsNullOrEmpty(_selectedCharacterName)
                ? _currentlySelectedCharacter.name
                : _selectedCharacterName;

            GameObject player = PhotonNetwork.Instantiate(
                prefabName,
                spawnPosition,
                spawnPoint.rotation,
                0,
                new object[] { _selectedCharacterName }
            );

            player.transform.rotation = Quaternion.Euler(0, spawnPoint.rotation.eulerAngles.y, 0);
            player.name = "Player_" + PhotonNetwork.LocalPlayer.UserId;

            if (sp != null)
            {
                var props = new Hashtable { { PlayerPropertyKeys.SpawnPoint, sp.Id } };
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            }

            return player;
        }

        // ==================== 重新生成 / 角色切换 ====================

        public void RespawnCharacter()
        {
            int currentSpawnPointId = -1;
            if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PlayerPropertyKeys.SpawnPoint))
            {
                currentSpawnPointId = (int)PhotonNetwork.LocalPlayer.CustomProperties[PlayerPropertyKeys.SpawnPoint];
            }

            if (_localPlayerObject != null)
            {
                PhotonNetwork.Destroy(_localPlayerObject);
            }

            GameObject newPlayer = RespawnAtSpawnPoint(currentSpawnPointId);

            if (newPlayer != null)
            {
                InitializePlayerSettings(newPlayer);
                SetupPlayerUI(newPlayer);
                Debug.Log($"[SpawnPointManager] 在原生成点 {currentSpawnPointId} 重新生成角色");
            }
        }

        private GameObject RespawnAtSpawnPoint(int spawnPointId)
        {
            ISpawnPoint targetSpawnPoint = ServiceLocator.Get<GameManager>().GetSpawnPointById(spawnPointId);

            if (targetSpawnPoint == null)
            {
                Debug.LogWarning($"[SpawnPointManager] 找不到ID为 {spawnPointId} 的生成点，使用第一个空闲生成点");
                return SpawnPlayer();
            }

            int localActorNumber = NetworkServiceLocator.PlayerService.GetLocalActorNumber();
            targetSpawnPoint.SetOccupied(true, localActorNumber);

            Transform spawnTransform = ((MonoBehaviour)targetSpawnPoint).transform;
            Vector3 spawnPosition = CalculateSpawnPosition(spawnTransform);

            // Phase 3: 使用角色名作为预制体名
            string prefabName = string.IsNullOrEmpty(_selectedCharacterName)
                ? _currentlySelectedCharacter.name
                : _selectedCharacterName;

            GameObject player = PhotonNetwork.Instantiate(
                prefabName,
                spawnPosition,
                spawnTransform.rotation,
                0,
                new object[] { _selectedCharacterName }
            );

            player.transform.rotation = Quaternion.Euler(0, spawnTransform.rotation.eulerAngles.y, 0);
            player.name = "Player_" + PhotonNetwork.LocalPlayer.UserId;

            return player;
        }

        public void SwitchCharacter(string newCharacterName)
        {
            // Phase 3: 存储角色名，不再修改预制体 name
            _selectedCharacterName = newCharacterName;
            RespawnCharacter();
        }

        // ==================== 生成点清理 ====================

        /// <summary>清理本地玩家的生成点占用（退出房间前调用）</summary>
        public void ClearLocalPlayerSpawnPoint()
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
                return;

            if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PlayerPropertyKeys.SpawnPoint))
            {
                int spawnPointId = (int)PhotonNetwork.LocalPlayer.CustomProperties[PlayerPropertyKeys.SpawnPoint];
                ISpawnPoint sp = ServiceLocator.Get<GameManager>().GetSpawnPointById(spawnPointId);
                if (sp != null && (sp as MonoBehaviour) != null && sp.GetOccupiedByPlayer() == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    sp.ForceRelease();
                    Debug.Log("[SpawnPointManager] 退出前释放生成点");
                }
            }
        }

        /// <summary>其他玩家离开房间时释放其生成点（由 Launcher.OnPlayerLeftRoom 委托）</summary>
        public void HandlePlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            Debug.Log($"[SpawnPointManager] 玩家 {otherPlayer.NickName} 离开房间");
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[SpawnPointManager] 连接状态异常，跳过生成点释放");
                return;
            }
            if (otherPlayer.CustomProperties.ContainsKey(PlayerPropertyKeys.SpawnPoint))
            {
                int spawnPointId = (int)otherPlayer.CustomProperties[PlayerPropertyKeys.SpawnPoint];
                ISpawnPoint sp = ServiceLocator.Get<GameManager>().GetSpawnPointById(spawnPointId);
                if (sp != null)
                {
                    sp.ForceRelease();
                    Debug.Log($"[SpawnPointManager] 释放玩家 {otherPlayer.NickName} 占用的生成点 {spawnPointId}");
                }
            }
        }

        /// <summary>Launcher OnDestroy 时清理本地生成点</summary>
        public void HandleDestroy()
        {
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PlayerPropertyKeys.SpawnPoint))
                {
                    int spawnPointId = (int)PhotonNetwork.LocalPlayer.CustomProperties[PlayerPropertyKeys.SpawnPoint];
                    ISpawnPoint sp = ServiceLocator.Get<GameManager>().GetSpawnPointById(spawnPointId);
                    if (sp != null && (sp as MonoBehaviour) != null)
                    {
                        sp.ForceRelease();
                    }
                }
            }
        }

        // ==================== 玩家初始化 ====================

        private void InitializePlayerSettings(GameObject player)
        {
            if (!EventChannelLocator.MainContainer.gameSettings.IsStayLobby)
            {
                GameObject vcam = player.transform.Find("VirtualCamera")?.gameObject;
                if (vcam != null)
                {
                    vcam.SetActive(true);
                }
            }
            else
            {
                player.transform.LookAt(
                    new Vector3(0.182999998f, player.transform.position.y, -0.219999999f)
                );
            }
        }

        private void SetupPlayerUI(GameObject player)
        {
            _localPlayer = PhotonNetwork.LocalPlayer;
            if (string.IsNullOrEmpty(_localPlayer.NickName))
                _localPlayer.NickName = "玩家" + _localPlayer.UserId;

            if (_nameUI != null)
            {
                _nameUI.text = _localPlayer.NickName;
            }

            var worldSpaceUI = player.GetComponentInChildren<WorldSpaceUI>();
            if (worldSpaceUI != null)
            {
                worldSpaceUI.UpDatePlayerName(_localPlayer.NickName);
            }

            _localPlayerObject = player;
        }

        // ==================== 辅助方法 ====================

        public void EnsurePlayerManagerExists()
        {
            if (ServiceLocator.Get<PlayerManager>() == null)
            {
                PlayerManager existingPM = Object.FindObjectOfType<PlayerManager>();
                if (existingPM == null)
                {
                    GameObject pmObj = new GameObject("PlayerManager");
                    pmObj.AddComponent<PlayerManager>();
                    Debug.Log("[SpawnPointManager] 创建 PlayerManager");
                }
            }
            else
            {
                EventChannelLocator.MainContainer.gameActionChannel.Raise(Core.Channels.General.GameActionType.SyncAllPlayers);
            }
        }

        private Vector3 CalculateSpawnPosition(Transform spawnPoint)
        {
            if (EventChannelLocator.MainContainer.gameSettings.IsStayLobby)
            {
                return spawnPoint.position;
            }

            Vector2 randomCircle = Random.insideUnitCircle * 10.0f;
            Vector3 randomOffset = new Vector3(randomCircle.x, spawnPoint.position.y, randomCircle.y);
            return spawnPoint.position + spawnPoint.TransformVector(randomOffset);
        }

        private void ApplySavedCharacter()
        {
            int savedIndex = SaveManager.SelectedCharacterIndex;

            string characterName = _characterInfoLibrary != null
                ? _characterInfoLibrary.GetNameByIndex(savedIndex)
                : string.Empty;

            if (string.IsNullOrEmpty(characterName))
            {
                Debug.LogWarning($"[SpawnPointManager] 未找到角色索引 {savedIndex} 对应的 Prefab 名称");
                return;
            }

            // Phase 3: 存储角色名（如 "WizardBoyRoot"），用于 PlayerCharacter 预制体实例化
            _selectedCharacterName = characterName;
            Debug.Log($"[SpawnPointManager] 应用保存的角色: {characterName}");
        }

        private void SetupRoomInfo()
        {
            GameObject roomNameObj = GameObject.Find("RoomName");
            if (roomNameObj != null && PhotonNetwork.CurrentRoom != null)
            {
                Text roomNameText = roomNameObj.GetComponent<Text>();
                if (roomNameText != null)
                {
                    roomNameText.text = PhotonNetwork.CurrentRoom.Name;
                }
            }
        }
    }
}
