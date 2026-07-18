using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core;
using Core.Channels.General;
using Controllers.Services;
using System.Collections.Generic;
using UI.Framework.Panel;
using Controllers.Network;

namespace Managers
{
    public class GameManager : MonoBehaviour, ISpawnPointService
    {
        public static GameManager instance;

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
            if (instance == null)
            {
                instance = this;
                ServiceLocator.Register(this);
                DomainServiceLocator.Register(this);
                DomainServiceLocator.Register<ISpawnPointService>(this);
                EventChannelLocator.MainContainer.gameSettings.IsStayLobby = isStayLobbyInspector;
                EventChannelLocator.MainContainer.gameSettings.IsTest = isTestInspector;
                DontDestroyOnLoad(gameObject);

                // ── 初始化 Lua 热更新环境 ──
                LuaEnvManager.Instance.Init();
                StartCoroutine(InitHotfix());
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private IEnumerator InitHotfix()
        {
            var hotfix = GetComponent<HotfixManager>();
            if (hotfix == null)
                hotfix = gameObject.AddComponent<HotfixManager>();

            yield return StartCoroutine(hotfix.DownloadAndLoad());
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            EventChannelLocator.MainContainer.gameActionChannel.RegisterListener(OnGameActionReceived);
            EventChannelLocator.MainContainer.bossDeathChannel?.RegisterListener(OnBossDeath);
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            EventChannelLocator.MainContainer.gameActionChannel.UnregisterListener(OnGameActionReceived);
            EventChannelLocator.MainContainer.bossDeathChannel?.UnregisterListener(OnBossDeath);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindSpawnPoints();

            // 进入大厅时自动加载存档
            if (scene.buildIndex == 1)
            {
                if (SaveManager.Instance != null)
                    SaveManager.Instance.LoadGame();
            }
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

        // 根据 ActorNumber 确定性地分配生成点，避免多玩家抢占同一位置
        public ISpawnPoint GetSpawnPointForPlayer(int actorNumber)
        {
            if (spawnPointDict.Count == 0) return null;

            var sortedIds = new List<int>(spawnPointDict.Keys);
            sortedIds.Sort();

            int index = (actorNumber - 1) % sortedIds.Count;
            return spawnPointDict[sortedIds[index]];
        }

        // 生成点状态变化时的回调
        public void OnSpawnPointStateChanged()
        {
            Debug.Log("[GameManager] 生成点状态已更新");
        }
        #endregion

        #region Boss死亡→返回大厅
        /// <summary>
        /// Boss死亡时触发：MasterClient 延迟10秒后自动切换回大厅场景
        /// </summary>
        private void OnBossDeath()
        {
            Debug.Log($"[GameManager] OnBossDeath 收到！IsMasterClient={NetworkServiceLocator.PlayerService?.IsMasterClient}, IsTest={EventChannelLocator.MainContainer.gameSettings.IsTest}");

            // 仅在 MasterClient 或测试模式下执行场景切换
            if (!EventChannelLocator.MainContainer.gameSettings.IsTest && !NetworkServiceLocator.PlayerService.IsMasterClient)
                return;

            Debug.Log("[GameManager] Boss已死亡，10秒后返回大厅...");
            StartCoroutine(DelayedLobbyTransition());
        }

        private IEnumerator DelayedLobbyTransition()
        {
            // 延迟10秒，让玩家观看死亡演出 + 结算缓冲
            yield return new WaitForSeconds(10f);

            // ★ 返回大厅前自动保存（对局结算数据）
            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveGame();

            Debug.Log("[GameManager] 正在返回大厅...");
            // 加载大厅场景（场景索引1）
            if (EventChannelLocator.MainContainer.gameSettings.IsTest)
            {
                SceneManager.LoadScene(1);
            }
            else
            {
                if (NetworkServiceLocator.PlayerService.IsMasterClient)
                {
                    NetworkServiceLocator.ObjectPoolService.ReturnToLobby();
                }
            }
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
        public const int BingNv = 1;
    }

    public class CharactorName
    {
        public const string WiZardBoy = "WizardBoyRoot";
        public const string BingNv = "BingNvRoot";
    }
}