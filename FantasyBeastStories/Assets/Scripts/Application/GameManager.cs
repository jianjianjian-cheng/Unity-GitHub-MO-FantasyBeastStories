using UnityEngine;
using UnityEngine.SceneManagement;
using Domain.Event;
using Domain.Event.Channels.General;
using Domain.Services;
using System.Collections.Generic;

namespace Application
{
    public class GameManager : MonoBehaviour, ISpawnPointService
    {
        public int sceneIndex = 2;
        public bool isReady = false;

        [SerializeField]
        public GameObject[] spawnPoints = { }; // 生成点列表
        private Dictionary<int, ISpawnPoint> spawnPointDict = new Dictionary<int, ISpawnPoint>();

        [SerializeField]
        private bool isStayLobbyInspector; // 在Inspector面板中设置的是否在大厅场景

        [SerializeField]
        private bool isTestInspector; // 在Inspector面板中设置的测试模式

        void Awake()
        {
            ServiceLocator.Register(this);
            DomainServiceLocator.Register(this);
            DomainServiceLocator.Register<ISpawnPointService>(this);
            EventChannelLocator.MainContainer.gameSettings.IsStayLobby = isStayLobbyInspector;
            EventChannelLocator.MainContainer.gameSettings.IsTest = isTestInspector;
            DontDestroyOnLoad(gameObject);
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            EventChannelLocator.MainContainer.gameActionChannel.RegisterListener(OnGameActionReceived);
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            EventChannelLocator.MainContainer.gameActionChannel.UnregisterListener(OnGameActionReceived);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindSpawnPoints();
        }

        #region 生成点管理
        public void FindSpawnPoints()
        {
            spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
            spawnPointDict.Clear();

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    ISpawnPoint sp = spawnPoints[i].GetComponent<ISpawnPoint>();
                    if (sp != null)
                    {
                        spawnPointDict[sp.Id] = sp;
                        Debug.Log($"[GameManager] 找到生成点: ID={sp.Id}, Name={spawnPoints[i].name}");
                    }
                }
            }
        }

        public GameObject GetEmptySpawnPoint()
        {
            foreach (GameObject spawnPoint in spawnPoints)
            {
                if (spawnPoint == null)
                    continue;

                ISpawnPoint sp = spawnPoint.GetComponent<ISpawnPoint>();
                if (sp == null)
                    continue;

                if (sp.IsEmpty())
                {
                    Debug.Log($"[GameManager] 返回空闲生成点: {spawnPoint.name}, ID={sp.Id}");
                    return spawnPoint;
                }
            }

            Debug.LogWarning("[GameManager] 没有空闲的生成点了");
            return null;
        }

        // 根据玩家 ActorNumber 获取其当前使用的生成点
        public ISpawnPoint GetSpawnPointByPlayer(int actorNumber)
        {
            foreach (var sp in spawnPointDict.Values)
            {
                if (sp.GetOccupiedByPlayer() == actorNumber)
                {
                    return sp;
                }
            }
            return null;
        }

        // 根据 ID 获取生成点
        public ISpawnPoint GetSpawnPointById(int id)
        {
            spawnPointDict.TryGetValue(id, out ISpawnPoint sp);
            return sp;
        }

        // 生成点状态变化时的回调
        public void OnSpawnPointStateChanged()
        {
            Debug.Log("[GameManager] 生成点状态已更新");
        }
        #endregion

        void OnGameActionReceived(GameActionType actionType)
        {
            switch (actionType)
            {
                case GameActionType.QuitToLobby:
                case GameActionType.QuitToMainMenu:
                    if (NetworkServiceLocator.GameActionService != null)
                        NetworkServiceLocator.GameActionService.QuitToMainMenu();
                    break;
                case GameActionType.SwitchCharacter:
                    break;
                case GameActionType.SetLocalReady:
                    if (NetworkServiceLocator.GameActionService != null)
                        NetworkServiceLocator.GameActionService.SetLocalReady(true);
                    break;
                case GameActionType.SyncAllPlayers:
                    break;
                default:
                    Debug.LogWarning($"未处理的游戏操作类型: {actionType}");
                    break;
            }
        }
    }

    public class CharactorIndex
    {
        public const int WiZardBoy = 0;
        public const int LittleRedGirl = 1;
    }

    public class CharactorName
    {
        public const string WiZardBoy = "WizardBoyRoot";
        public const string LittleRedGirl = "LittleRedGirlRoot";
    }
}