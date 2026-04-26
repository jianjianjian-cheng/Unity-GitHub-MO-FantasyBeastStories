using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Manager;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.VisualScripting;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using ExitGames.Client.Photon.StructWrapping;
using Other;
using Charactors;

public class Launcher : MonoBehaviourPunCallbacks
{
    private bool allPlayersLoaded = false;
    private bool isAutoCreate = false;
    private bool isJoiningRoom = false;
    [SerializeField] public GameObject currentlySelectedCharacter;
    private Photon.Realtime.Player localPlayer;
    private InputField nameUI;
    private GameObject joinRoom;
    private GameObject TBGC;
    private GameObject joinRoomButton;
    private GameObject joinRoomInput;

    private bool isLoadingScene = false;

    #region 单例模式
    public static Launcher instance;
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

    private bool isTest;
    private bool isRoomLoading = false;

    void Start()
    {
        isTest = GameManager.instance != null && GameManager.isTest;
        if (isTest) return;
        PhotonNetwork.AutomaticallySyncScene = true;
        Application.runInBackground = true;
        PhotonNetwork.ConnectUsingSettings();
        Initialize();
    }

    private void Update()
    {
        if (isTest) return;
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (string.IsNullOrEmpty(nameUI.text))
            {
                Debug.LogError("Name is empty");
                return;
            }
            else
            {
                localPlayer.NickName = nameUI.text;
                Debug.Log("修改昵称成功");
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TBGC.SetActive(false);
            joinRoomButton.SetActive(false);
            joinRoomInput.SetActive(false);
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    override public void OnDisable()
    {
        base.OnDisable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public override void OnConnectedToMaster()
    {
        if (isTest) return;
        base.OnConnectedToMaster();
        Debug.Log("Connected to master");
        string roomName = "Room_" + Random.Range(1000, 9999);
        if (!isAutoCreate)
        {
            PhotonNetwork.CreateRoom(roomName, new Photon.Realtime.RoomOptions() { MaxPlayers = 4 }, default);
            isAutoCreate = true;
        }

        if (isJoiningRoom)
        {
            PhotonNetwork.JoinRoom(pendingRoomName);
            Debug.Log("加入房间成功");
            isJoiningRoom = false;
        }
    }

    private void Initialize()
    {
        nameUI = GameObject.Find("NameUI").GetComponent<InputField>();
        TBGC = GetInactiveObjectByName("TBGC");
        joinRoomButton = GetInactiveObjectByName("JoinRoomButton");
        joinRoomInput = GetInactiveObjectByName("JoinRoomInput");
        joinRoom = GameObject.Find("JoinRoom");

        joinRoom.GetComponent<Button>().onClick.AddListener(() =>
        {
            TBGC.SetActive(true);
            joinRoomButton.SetActive(true);
            joinRoomInput.SetActive(true);
        });

        joinRoomButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            string roomName = joinRoomInput.GetComponent<InputField>().text;
            if (string.IsNullOrEmpty(roomName))
            {
                Debug.LogError("Room name is empty");
                return;
            }
            {
                SwitchRoom(roomName);
            }
            TBGC.SetActive(false);
            joinRoomButton.SetActive(false);
            joinRoomInput.SetActive(false);
        });
    }

    private string pendingRoomName = "";

    public void SwitchRoom(string newRoomName)
    {
        if (PhotonNetwork.InRoom)
        {
            pendingRoomName = newRoomName;
            isJoiningRoom = true;
            PhotonNetwork.LeaveRoom();
            Debug.Log("正在离开当前房间...");
        }
    }

    public void SetLocalReady(bool ready)
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("无法设置准备状态：未在房间中");
            return;
        }

        Hashtable props = new Hashtable { { "PlayerReady", ready } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        Debug.Log($"[Launcher] 本地玩家准备状态: {ready} - {PhotonNetwork.LocalPlayer.NickName}");

        // 移除房主检查，让所有玩家都能检查
        CheckAllPlayersReady();
    }

    private bool AllPlayersReady()
    {
        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.ContainsKey("PlayerReady") ||
                (bool)player.CustomProperties["PlayerReady"] == false)
            {
                return false;
            }
        }
        return true;
    }

    private void CheckAllPlayersReady()
    {
        // 移除房主限制，让所有玩家都能触发检查
        if (isRoomLoading || isLoadingScene)
        {
            return;
        }

        if (AllPlayersReady())
        {
            Debug.Log("所有玩家已准备，开始加载场景");
            isRoomLoading = true;

            if (LoadingCanvas.instance != null)
            {
                LoadingCanvas.instance.ShowLoading();
            }

            // 修改：不要在这里启动协程，而是直接加载场景
            // PhotonNetwork.LoadLevel 会自动同步给所有玩家
            StartCoroutine(LoadLevelAfterDelay());
        }
    }

    // 新增：延迟加载场景的协程
    IEnumerator LoadLevelAfterDelay()
    {
        isLoadingScene = true;
        yield return new WaitForSeconds(2f);

        // 只有房主加载场景
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(2);
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("已离开房间");
        if (!string.IsNullOrEmpty(pendingRoomName))
        {
            Debug.Log($"正在加入房间: {pendingRoomName}");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (GameManager.isTest) return;
        if (scene.buildIndex == 1)
        {
            GameManager.isStayLobby = true;
            Initialize();
            return;
        }

        if (scene.buildIndex > 1)
        {
            GameManager.isStayLobby = false;
            // 玩家进入游戏场景后标记已加载完成
            Hashtable loadProps = new Hashtable
            {
                { "PlayerLevel", true },
                { "PlayerLoaded", true },
                { "playerName", localPlayer.NickName }
            };

            PhotonNetwork.LocalPlayer.SetCustomProperties(loadProps);
            Debug.Log($"[Launcher] 本地玩家场景加载完成，设置 PlayerLoaded=true: {PhotonNetwork.LocalPlayer.NickName}");

            CreatedOrJoinedRoom();
        }
    }


    public override void OnCreatedRoom()
    {
        if (isTest) return;
        base.OnCreatedRoom();
        Debug.Log("Created room: " + PhotonNetwork.CurrentRoom.Name);
    }

    public void CreatedOrJoinedRoom()
    {
        GameManager.instance.FindSpawnPoints();
        Debug.Log("执行CreatedOrJoinedRoom");

        // 生成玩家
        GameObject player = SpawnPlayer();

        if (player != null)
        {
            // 初始化玩家设置
            InitializePlayerSettings(player);

            // 设置玩家UI
            SetupPlayerUI(player);
        }

        // 隐藏加载界面
        HideLoadingCanvas();

        // 设置房间信息
        SetupRoomInfo();
    }

    /// <summary>
    /// 生成玩家角色
    /// </summary>
    private GameObject SpawnPlayer()
    {
        Transform spawnPoint = GameManager.instance.GetEmptySpawnPoint()?.transform;

        if (spawnPoint == null)
        {
            Debug.LogError("[Launcher] 没有可用的生成点！");
            return null;
        }

        Vector3 spawnPosition = CalculateSpawnPosition(spawnPoint);

        // 生成玩家
        GameObject player = PhotonNetwork.Instantiate(
            currentlySelectedCharacter.name,
            spawnPosition,
            spawnPoint.rotation
        );

        player.transform.rotation = Quaternion.Euler(0, spawnPoint.rotation.eulerAngles.y, 0);
        player.name = "Player_" + PhotonNetwork.LocalPlayer.UserId;

        // 记录当前使用的生成点ID到玩家属性
        SpawnPoint sp = spawnPoint.GetComponent<SpawnPoint>();
        if (sp != null)
        {
            var props = new Hashtable
        {
            { "CurrentSpawnPoint", sp.Id }
        };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        return player;
    }


    /// <summary>
    /// 重新生成角色（用于切换角色）
    /// </summary>
    public void RespawnCharacter()
    {
        // 获取玩家当前使用的生成点ID
        int currentSpawnPointId = -1;
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("CurrentSpawnPoint"))
        {
            currentSpawnPointId = (int)PhotonNetwork.LocalPlayer.CustomProperties["CurrentSpawnPoint"];
        }

        // 销毁当前角色
        if (localPlayerObject != null)
        {
            PhotonNetwork.Destroy(localPlayerObject);
        }

        // 在原生成点生成新角色
        GameObject newPlayer = RespawnAtSpawnPoint(currentSpawnPointId);

        if (newPlayer != null)
        {
            InitializePlayerSettings(newPlayer);
            SetupPlayerUI(newPlayer);
            Debug.Log($"[Launcher] 在原生成点 {currentSpawnPointId} 重新生成角色");
        }
    }

    // 在指定生成点生成角色
    private GameObject RespawnAtSpawnPoint(int spawnPointId)
    {
        SpawnPoint targetSpawnPoint = GameManager.instance.GetSpawnPointById(spawnPointId);

        if (targetSpawnPoint == null)
        {
            Debug.LogWarning($"[Launcher] 找不到ID为 {spawnPointId} 的生成点，使用第一个空闲生成点");
            return SpawnPlayer();
        }

        Transform spawnTransform = targetSpawnPoint.transform;
        Vector3 spawnPosition = CalculateSpawnPosition(spawnTransform);

        GameObject player = PhotonNetwork.Instantiate(
            currentlySelectedCharacter.name,
            spawnPosition,
            spawnTransform.rotation
        );

        player.transform.rotation = Quaternion.Euler(0, spawnTransform.rotation.eulerAngles.y, 0);
        player.name = "Player_" + PhotonNetwork.LocalPlayer.UserId;

        return player;
    }

    // 添加玩家退出时的清理逻辑
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log($"[Launcher] 玩家 {otherPlayer.NickName} 离开房间");
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[Launcher] 连接状态异常，跳过生成点释放");
            return;
        }
        // 释放该玩家占用的生成点
        if (otherPlayer.CustomProperties.ContainsKey("CurrentSpawnPoint"))
        {
            int spawnPointId = (int)otherPlayer.CustomProperties["CurrentSpawnPoint"];
            SpawnPoint sp = GameManager.instance.GetSpawnPointById(spawnPointId);
            if (sp != null)
            {
                sp.ForceRelease();
                Debug.Log($"[Launcher] 释放玩家 {otherPlayer.NickName} 占用的生成点 {spawnPointId}");
            }
        }
    }

    // 修改 OnPlayerPropertiesUpdate 以处理生成点状态
    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("PlayerLevel"))
        {
            Debug.Log($"玩家 {targetPlayer.NickName} 的 PlayerLevel 属性已更新: {changedProps["PlayerLevel"]}");
        }

        if (changedProps.ContainsKey("PlayerReady"))
        {
            CheckAllPlayersReady();
        }

        if (changedProps.ContainsKey("PlayerLoaded"))
        {
            CheckAllPlayersLoaded();
        }

        // 处理生成点属性变化
        if (changedProps.ContainsKey("CurrentSpawnPoint"))
        {
            object spawnPointValue = changedProps["CurrentSpawnPoint"];
            Debug.Log($"玩家 {targetPlayer.NickName} 的生成点更新: {spawnPointValue}");
        }
    }

    /// <summary>
    /// 计算生成位置
    /// </summary>
    private Vector3 CalculateSpawnPosition(Transform spawnPoint)
    {
        if (GameManager.isStayLobby)
        {
            return spawnPoint.position;
        }

        // 在生成点周围XZ平面内随机生成
        Vector2 randomCircle = Random.insideUnitCircle * 10.0f;
        Vector3 randomOffset = new Vector3(randomCircle.x, spawnPoint.position.y, randomCircle.y);
        return spawnPoint.position + spawnPoint.TransformVector(randomOffset);
    }

    /// <summary>
    /// 初始化玩家设置
    /// </summary>
    private void InitializePlayerSettings(GameObject player)
    {
        if (!GameManager.isStayLobby)
        {
            // 激活虚拟摄像机
            GameObject vcam = player.transform.Find("VirtualCamera")?.gameObject;
            if (vcam != null)
            {
                vcam.SetActive(true);
            }
        }
        else
        {
            // 在大厅中看向指定方向
            player.transform.LookAt(new Vector3(0.182999998f, player.transform.position.y, -0.219999999f));
        }
    }

    /// <summary>
    /// 设置玩家UI
    /// </summary>
    private void SetupPlayerUI(GameObject player)
    {
        localPlayer = PhotonNetwork.LocalPlayer;
        localPlayer.NickName = "玩家" + localPlayer.UserId;

        // 设置本地UI
        if (nameUI != null)
        {
            nameUI.text = localPlayer.NickName;
        }

        // 设置世界空间UI
        var worldSpaceUI = player.GetComponentInChildren<WordlSpaceUI>();
        if (worldSpaceUI != null)
        {
            worldSpaceUI.UpDatePlayerName(localPlayer.NickName);
        }

        localPlayerObject = player;
    }

    /// <summary>
    /// 隐藏加载画布
    /// </summary>
    private void HideLoadingCanvas()
    {
        if (LoadingCanvas.instance != null)
        {
            LoadingCanvas.instance.HideLoading();
        }
    }

    /// <summary>
    /// 设置房间信息显示
    /// </summary>
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

    // 添加一个字段来保存当前玩家对象的引用
    private GameObject localPlayerObject;

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room: " + PhotonNetwork.CurrentRoom.Name);
        if (isTest) return;
        base.OnJoinedRoom();
        StartCoroutine(DelayJoinRoom());
    }

    IEnumerator DelayJoinRoom()
    {
        yield return new WaitForSeconds(0.5f);
        CreatedOrJoinedRoom();
    }

    public GameObject GetInactiveObjectByName(string objectName)
    {
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        return System.Array.Find(allObjects, obj =>
            obj != null && // 过滤null对象
            obj.name == objectName &&
            !obj.activeInHierarchy &&
            obj.scene.IsValid() // 确保是场景中的物体，不是预制体资源
        );
    }

    // 切换角色示例
    public void SwitchCharacter(string newCharacterName)
    {
        this.currentlySelectedCharacter.name = newCharacterName;
        RespawnCharacter();
    }

    public void ResetSpawnPointState(int spawnPointIndex)
    {
        foreach (var spawnPoint in GameManager.instance.spawnPoints)
        {
            if (spawnPoint.GetComponent<SpawnPoint>().Id == spawnPointIndex)
            {

            }
        }
    }

    private void CheckAllPlayersLoaded()
    {
        // 添加防止重复加载的逻辑
        if (isLoadingScene || allPlayersLoaded)
        {
            return;
        }

        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.ContainsKey("PlayerLoaded") ||
                (bool)player.CustomProperties["PlayerLoaded"] == false)
            {
                Debug.Log($"等待玩家 {player.NickName} 加载场景...");
                return;
            }
        }

        if (LoadingCanvas.instance != null)
        {
            LoadingCanvas.instance.HideLoading();
        }

        allPlayersLoaded = true;
        Debug.Log("所有玩家已加载完成，开始游戏！");
    }
}