using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core;
using Core.Channels.General;
using Core.Contracts;
using Core.Network;
using System.Collections.Generic;
using UI.Framework.Panel;
using Controllers.Network;
using Core.Audio;

namespace Managers
{
    public class GameManager : MonoBehaviour, ISpawnPointService
    {
        public static GameManager instance;

        public int sceneIndex = 2;
        public bool isReady = false;

        [Header("场景配置")]
        [SerializeField] private SceneConfigSO sceneConfig;

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
                ServiceLocator.Register<ISpawnPointService>(this);
                EventChannelLocator.MainContainer.gameSettings.IsStayLobby = isStayLobbyInspector;
                EventChannelLocator.MainContainer.gameSettings.IsTest = isTestInspector;
                DontDestroyOnLoad(gameObject);

                SetCustomCursor();

                // ── 等待 Addressables 热更预下载完成后再初始化 ──
                StartCoroutine(InitAfterUpdate());
            }
            else
            {
                Destroy(gameObject);
            }
        }

        [Header("鼠标指针")]
        [SerializeField] private Texture2D cursorTexture;
        [SerializeField] private Vector2 cursorHotspot = Vector2.zero;

        private void SetCustomCursor()
        {
            if (cursorTexture == null)
                cursorTexture = Core.AssetLoader.TryLoadAsset<Texture2D>("Assets/_Project/Art/Textures/UI/Cursor/GameCursor.png");
            if (cursorTexture != null)
            {
                Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
                Debug.Log($"[GameManager] 自定义光标已设置: {cursorTexture.name}");
            }
            else
            {
                Debug.LogWarning("[GameManager] 未能加载自定义光标纹理，使用系统默认");
            }
        }

        private bool isInBattle = false;

        private void Update()
        {
            UpdateCursorVisibility();
        }

        private void UpdateCursorVisibility()
        {
            if (!isInBattle)
            {
                if (!Cursor.visible) Cursor.visible = true;
                return;
            }

            // 战斗场景：Ctrl 按住 或 卡牌选择面板打开时显示鼠标
            bool showCursor = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (!showCursor && UI.MagicUpgradeManager.instance != null && UI.MagicUpgradeManager.instance.IsPanelActive)
                showCursor = true;

            if (Cursor.visible != showCursor)
                Cursor.visible = showCursor;
        }

        /// <summary>等待 Addressables 热更预下载完成后初始化 Lua 环境</summary>
        private IEnumerator InitAfterUpdate()
        {
            // 等待 AddressablesUpdater 完成（devMode 下瞬间完成）
            yield return new WaitUntil(() => AddressablesUpdater.IsUpdateComplete);

            LuaEnvManager.Instance.Init();
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

            isInBattle = scene.buildIndex == sceneConfig.battleSceneIndex;

            // 进入大厅时自动加载存档
            if (scene.buildIndex == sceneConfig.lobbySceneIndex)
            {
                Cursor.visible = true;

                if (SaveManager.Instance != null)
                    SaveManager.Instance.LoadGame();

                // 大厅 BGM（与 LobbyAllGameManager/LobbyCanvas 中的调用去重）
                AudioManager.Instance?.PlayBGM("bgm_main_menu");
            }
            else if (scene.buildIndex == sceneConfig.battleSceneIndex)
            {
                // 战斗场景 BGM
                AudioManager.Instance?.PlayBGM("bgm_combat");
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

        /// <summary>
        /// 公开入口：触发延迟返回大厅流程（供 SpectatorCameraController 全灭时调用）
        /// </summary>
        /// <param name="delay">延迟秒数，默认10秒</param>
        public void TriggerLobbyTransition(float delay = 10f)
        {
            Debug.Log($"[GameManager] TriggerLobbyTransition 收到全灭信号，{delay}秒后返回大厅...");
            StartCoroutine(DelayedLobbyTransition(delay));
        }

        private IEnumerator DelayedLobbyTransition(float delay = 10f)
        {
            // 延迟，让玩家观看死亡演出 + 结算缓冲
            yield return new WaitForSeconds(delay);

            // ★ 返回大厅前自动保存（对局结算数据）
            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveGame();

            Debug.Log("[GameManager] 正在返回大厅...");

            if (EventChannelLocator.MainContainer.gameSettings.IsTest)
            {
                SceneManager.LoadScene(sceneConfig.lobbySceneIndex);
            }
            else if (NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                // 房主通过 RPC 通知所有客户端播放加载动画并返回大厅
                NetworkServiceLocator.ObjectService.InvokeRPC(
                    ManagerRpcBridge.Instance,
                    "RPC_ReturnToLobby",
                    NetworkTarget.All
                );
            }
            else
            {
                // 非主机：播放加载动画，等待房主同步场景
                if (Loading.Instance != null)
                    yield return Loading.Instance.Show();
                Debug.Log("[GameManager] 非主机等待房主同步场景...");
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

    [System.Obsolete("使用 CharacterInfoLibrarySO.GetNameByIndex 替代")]
    public class CharactorIndex
    {
        public const int WiZardBoy = 0;
        public const int BingNv = 1;
    }

    [System.Obsolete("使用 CharacterInfoLibrarySO.GetNameByIndex 替代")]
    public class CharactorName
    {
        public const string WiZardBoy = "WizardBoyRoot";
        public const string BingNv = "BingNvRoot";
    }
}