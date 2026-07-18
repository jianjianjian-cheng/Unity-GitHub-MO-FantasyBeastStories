using System.Collections;
using System.Collections.Generic;
using Controllers.Character;
using Core;
using Controllers.Player;
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.StructWrapping;
using Photon.Pun;
using UI.Framework.Panel;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Managers;
using UI;
using Controllers.Services;

namespace Controllers.Network
{
  public class Launcher : MonoBehaviourPunCallbacks, IObjectPoolService, IGameActionService
  {
    private bool allPlayersLoaded = false;
    private bool isAutoCreate = false;
    private bool isJoiningRoom = false;

    private bool isQuittingToMenu = false;

    [SerializeField]
    public GameObject currentlySelectedCharacter;
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
        NetworkServiceLocator.RegisterObjectPoolService(this);
        NetworkServiceLocator.RegisterGameActionService(this);
      }
      else
      {
        Destroy(gameObject);
      }
      // 只在大厅场景（场景索引1）初始化UI
      if (SceneManager.GetActiveScene().buildIndex == 1)
      {
        InitGetUI();
      }
    }
    #endregion

    private bool isTest;
    private bool isRoomLoading = false;

    void Start()
    {
      isTest = EventChannelLocator.MainContainer != null && EventChannelLocator.MainContainer.gameSettings != null && EventChannelLocator.MainContainer.gameSettings.IsTest;
      if (isTest)
        return;
      PhotonNetwork.AutomaticallySyncScene = true;
      UnityEngine.Application.runInBackground = true;
      PhotonNetwork.ConnectUsingSettings();
    }

    private void Update()
    {
      if (isTest)
        return;
      if (Input.GetKeyDown(KeyCode.Return))
      {
        ConfirmNickname();
      }
      if (Input.GetKeyDown(KeyCode.Escape))
      {
        CloseJoinRoomPanel();
      }
    }

    /// <summary>
    /// 确认昵称（供 UI 按钮调用）
    /// </summary>
    public void ConfirmNickname()
    {
      if (string.IsNullOrEmpty(nameUI.text))
      {
        Debug.LogError("Name is empty");
        return;
      }
      else
      {
        PhotonNetwork.LocalPlayer.NickName = nameUI.text;
        Debug.Log($"修改昵称成功: {PhotonNetwork.LocalPlayer.NickName}");
      }
    }

    /// <summary>
    /// 关闭加入房间面板（供 UI 按钮调用）
    /// </summary>
    public void CloseJoinRoomPanel()
    {
      if (TBGC != null) TBGC.SetActive(false);
      if (joinRoomButton != null) joinRoomButton.SetActive(false);
      if (joinRoomInput != null) joinRoomInput.SetActive(false);
    }

    public override void OnEnable()
    {
      base.OnEnable();
      SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable()
    {
      base.OnDisable();
      SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public override void OnConnectedToMaster()
    {
      if (isTest || SceneManager.GetActiveScene().buildIndex < 1)
        return;
      base.OnConnectedToMaster();
      PhotonNetwork.AutomaticallySyncScene = true; // 开启自动同步场景
      Debug.Log("Connected to master");
      string roomName = "Room_" + Random.Range(1000, 9999);
      SetDefaultName();
      if (!isAutoCreate)
      {
        PhotonNetwork.CreateRoom(
            roomName,
            new Photon.Realtime.RoomOptions() { MaxPlayers = 4, PublishUserId = true },
            default
        );
        isAutoCreate = true;
      }

      if (isJoiningRoom)
      {
        PhotonNetwork.JoinRoom(pendingRoomName);
        Debug.Log("加入房间成功");
        isJoiningRoom = false;
      }
    }

    /// <summary>
    /// 设置默认昵称（后面修改为从数据库获取）
    /// </summary>
    private void SetDefaultName()
    {
      if (string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.NickName))
      {
        PhotonNetwork.LocalPlayer.NickName = "玩家" + PhotonNetwork.LocalPlayer.UserId;
      }
    }

    private void InitGetUI()
    {
      Debug.LogWarning("开始初始化UI组件---Launcher");
      nameUI = GameObject.Find("NameUI")?.GetComponent<InputField>();
      TBGC = GetInactiveObjectByName("TBGC");
      joinRoomButton = GetInactiveObjectByName("JoinRoomButton");
      joinRoomInput = GetInactiveObjectByName("JoinRoomInput");
      joinRoom = GameObject.Find("JoinRoom");
      Initialize();
    }

    private void Initialize()
    {
      joinRoom
          .GetComponent<Button>()
          .onClick.AddListener(() =>
          {
            TBGC.SetActive(true);
            joinRoomButton.SetActive(true);
            joinRoomInput.SetActive(true);
          });

      joinRoomButton
          .GetComponent<Button>()
          .onClick.AddListener(() =>
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
            CloseJoinRoomPanel();
          });

      // 自动绑定 TBGC 下的关闭按钮（如果有）
      if (TBGC != null)
      {
        Button closeBtn = TBGC.GetComponentInChildren<Button>(true);
        // 排除 joinRoomButton 自身，避免循环
        if (closeBtn != null && closeBtn.gameObject != joinRoomButton)
        {
          closeBtn.onClick.AddListener(CloseJoinRoomPanel);
        }
      }
    }

    private string pendingRoomName = "";

    //切换房间所使用的方法
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

    /// <summary>
    /// 退出房间并返回开始界面
    /// </summary>
    public void QuitToMainMenu()
    {
      if (PhotonNetwork.InRoom)
      {
        // 在退出前禁用场景同步，避免后续回调报错
        PhotonNetwork.AutomaticallySyncScene = false;
        // 在退出前手动清理生成点占用
        ClearLocalPlayerSpawnPoint();
        isQuittingToMenu = true;
        isAutoCreate = false;
        PhotonNetwork.LeaveRoom();
        Debug.Log("正在退出房间返回主菜单...");
      }
      else
      {
        // 不在房间中，直接加载开始界面（需先显示 loading）
        StartCoroutine(ShowAndLoadMainMenu());
      }
    }

    private IEnumerator ShowAndLoadMainMenu()
    {
      yield return StartCoroutine(Loading.Instance.Show());
      LoadMainMenuScene();
    }

    /// <summary>
    /// 加载开始界面场景
    /// </summary>
    private void LoadMainMenuScene()
    {
      // 重置状态标志
      isQuittingToMenu = false;
      pendingRoomName = "";
      isJoiningRoom = false;
      isAutoCreate = false;


      // 禁用自动同步场景，避免 Photon 干扰菜单场景
      PhotonNetwork.AutomaticallySyncScene = false;

      // 加载开始界面（假设场景索引为0或1，根据你的实际设置调整）
      SceneManager.LoadScene(0); // 或 SceneManager.LoadScene("MainMenu") 使用场景名称
    }

    /// <summary>
    /// 设置本地玩家的准备状态
    /// </summary>
    /// <param name="ready"></param>
    public void SetLocalReady(bool ready)
    {
      if (!PhotonNetwork.InRoom)
      {
        Debug.LogWarning("无法设置准备状态：未在房间中");
        return;
      }

      Hashtable props = new Hashtable { { PlayerPropertyKeys.Ready, ready } };
      PhotonNetwork.LocalPlayer.SetCustomProperties(props);
      Debug.Log($"[Launcher] 本地玩家准备状态: {ready} - {PhotonNetwork.LocalPlayer.NickName}");

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

      // 移除房主检查，让所有玩家都能检查
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

        StartCoroutine(LoadLevelAfterDelay());
      }
    }

    // 新增：延迟加载场景的协程
    public IEnumerator LoadLevelAfterDelay(int index = 2)
    {
      isLoadingScene = true;
      yield return new WaitForSeconds(2f);

      yield return StartCoroutine(Loading.Instance.Show());
      // 只有房主加载场景
      if (PhotonNetwork.IsMasterClient)
      {
        PhotonNetwork.LoadLevel(index);
      }
    }

    public override void OnLeftRoom()
    {
      Debug.Log("已离开房间");
      // 如果是退出到主菜单
      if (isQuittingToMenu)
      {
        StartCoroutine(ShowAndLoadMainMenu());
        return;
      }
      if (!string.IsNullOrEmpty(pendingRoomName))
      {
        Debug.Log($"正在加入房间: {pendingRoomName}");
      }
    }

    /// <summary>
    /// 清理本地玩家的生成点占用
    /// </summary>
    private void ClearLocalPlayerSpawnPoint()
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
          Debug.Log("[Launcher] 退出前释放生成点");
        }
      }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
      //初始化UI，避免切换场景时丢失UI
      if (scene.buildIndex == 1)
      {
        // LobbyCanvas 负责大厅 UI 初始化 — 通过事件通道通知
        var roomJoinedChannel = EventChannelLocator.MainContainer.roomJoinedChannel;
        if (roomJoinedChannel != null)
          roomJoinedChannel.Raise(new RoomJoinedEventData());
      }

      if (EventChannelLocator.MainContainer.gameSettings.IsTest)
        return;
      if (scene.buildIndex == 1)
      {
        EventChannelLocator.MainContainer.gameSettings.IsStayLobby = true;
        InitGetUI();
        // 重置加载标志
        isRoomLoading = false;
        isLoadingScene = false;
        allPlayersLoaded = false;

        // 关闭加载画面（返回大厅时 Loading.Show() 已提前打开）
        // Loading.Instance.Hide();

        // 重置本地玩家的 PlayerReady 属性（关键修复）
        if (
            PhotonNetwork.IsConnected
            && PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PlayerPropertyKeys.Ready)
        )
        {
          Hashtable props = new Hashtable { { PlayerPropertyKeys.Ready, false } };
          PhotonNetwork.LocalPlayer.SetCustomProperties(props);
          Debug.Log("[Launcher] 重置本地玩家就绪状态为 false");
        }

        // 在大厅场景生成角色（保持房间连接）
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
          // 延迟一帧确保场景完全加载
          StartCoroutine(SpawnPlayerAfterDelay());
        }

        // 新增：检查是否已连接到Master，如果是则尝试自动创建房间
        if (PhotonNetwork.IsConnected && !PhotonNetwork.InRoom && !isAutoCreate)
        {
          PhotonNetwork.AutomaticallySyncScene = true;
          string roomName = "Room_" + Random.Range(1000, 9999);
          SetDefaultName();
          PhotonNetwork.CreateRoom(
              roomName,
              new Photon.Realtime.RoomOptions() { MaxPlayers = 4, PublishUserId = true },
              default
          );
          isAutoCreate = true;
        }
        return;
      }

      if (scene.buildIndex > 1)
      {
        ServiceLocator.Get<GameManager>().isReady = false;
        EventChannelLocator.MainContainer.gameSettings.IsStayLobby = false;
        // 玩家进入游戏场景后标记已加载完成
        Hashtable loadProps = new Hashtable
            {
                { PlayerPropertyKeys.Level, true },
                { PlayerPropertyKeys.Loaded, true },
                { PlayerPropertyKeys.PlayerName, localPlayer.NickName },
            };

        PhotonNetwork.LocalPlayer.SetCustomProperties(loadProps);
        Debug.Log(
            $"[Launcher] 本地玩家场景加载完成，设置 PlayerLoaded=true: {PhotonNetwork.LocalPlayer.NickName}"
        );

        CreatedOrJoinedRoom();
      }
    }

    /// <summary>
    /// 延迟生成角色
    /// </summary>
    private IEnumerator SpawnPlayerAfterDelay()
    {
      yield return new WaitForEndOfFrame();

      // 确保生成点已初始化
      yield return new WaitUntil(() =>
          ServiceLocator.Get<GameManager>() != null && ServiceLocator.Get<GameManager>().GetEmptySpawnPoint() != null
      );

      //应用存档角色
      ApplySavedCharacter();


      // 生成玩家角色
      GameObject player = SpawnPlayer();
      if (player != null)
      {
        InitializePlayerSettings(player);
        SetupPlayerUI(player);
        Debug.Log("[Launcher] 在大厅重新生成角色");
      }
    }

    public override void OnCreatedRoom()
    {
      if (isTest)
        return;
      base.OnCreatedRoom();
      Debug.Log("Created room: " + PhotonNetwork.CurrentRoom.Name);
    }

    public void CreatedOrJoinedRoom()
    {
      if (SceneManager.GetActiveScene().buildIndex < 1)
      {
        Debug.Log("[Launcher] 当前不是游戏场景，跳过玩家生成");
        return;
      }
      ServiceLocator.Get<GameManager>().FindSpawnPoints();
      Debug.Log("执行CreatedOrJoinedRoom");

      // 确保PlayerManager存在
      EnsurePlayerManagerExists();

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
      // HideLoading();

      // 设置房间信息
      SetupRoomInfo();
    }

    // 确保PlayerManager存在
    private void EnsurePlayerManagerExists()
    {
      if (PlayerManager.instance == null)
      {
        // 先检查场景中是否已经存在
        PlayerManager existingPM = FindObjectOfType<PlayerManager>();
        if (existingPM == null)
        {
          GameObject pmObj = new GameObject("PlayerManager");
          pmObj.AddComponent<PlayerManager>();
          Debug.Log("[Launcher] 创建PlayerManager");
        }
      }
      else
      {
        // PlayerManager已存在，强制同步
        EventChannelLocator.MainContainer.gameActionChannel.Raise(Core.Channels.General.GameActionType.SyncAllPlayers);
      }
    }

    /// <summary>
    /// 生成玩家角色
    /// </summary>
    private GameObject SpawnPlayer()
    {
      int localActorNumber = NetworkServiceLocator.PlayerService.GetLocalActorNumber();
      var gameManager = ServiceLocator.Get<GameManager>();

      // 根据 ActorNumber 确定性地选择生成点，避免多玩家同时抢占同一位置
      ISpawnPoint sp = gameManager.GetSpawnPointForPlayer(localActorNumber);
      Transform spawnPoint = (sp as MonoBehaviour)?.transform;

      // 回退：若确定性分配的生成点已被占用，尝试获取任意空闲生成点
      if (sp == null || !sp.IsEmpty())
      {
        sp = gameManager.GetEmptySpawnPoint()?.GetComponent<ISpawnPoint>();
        spawnPoint = (sp as MonoBehaviour)?.transform;
      }

      if (spawnPoint == null)
      {
        Debug.LogError("[Launcher] 没有可用的生成点！");
        return null;
      }

      // ★ 立即标记生成点为占用状态并 RPC 同步，防止其他玩家也选到这个点
      if (sp != null)
      {
        sp.SetOccupied(true, localActorNumber);
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
      if (sp != null)
      {
        var props = new Hashtable { { PlayerPropertyKeys.SpawnPoint, sp.Id } };
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
      if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PlayerPropertyKeys.SpawnPoint))
      {
        currentSpawnPointId = (int)
            PhotonNetwork.LocalPlayer.CustomProperties[PlayerPropertyKeys.SpawnPoint];
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
      ISpawnPoint targetSpawnPoint = ServiceLocator.Get<GameManager>().GetSpawnPointById(spawnPointId);

      if (targetSpawnPoint == null)
      {
        Debug.LogWarning(
            $"[Launcher] 找不到ID为 {spawnPointId} 的生成点，使用第一个空闲生成点"
        );
        return SpawnPlayer();
      }

      // 立即标记占用，防止被其他玩家抢走
      int localActorNumber = NetworkServiceLocator.PlayerService.GetLocalActorNumber();
      targetSpawnPoint.SetOccupied(true, localActorNumber);

      Transform spawnTransform = ((MonoBehaviour)targetSpawnPoint).transform;
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
      if (otherPlayer.CustomProperties.ContainsKey(PlayerPropertyKeys.SpawnPoint))
      {
        int spawnPointId = (int)otherPlayer.CustomProperties[PlayerPropertyKeys.SpawnPoint];
        ISpawnPoint sp = ServiceLocator.Get<GameManager>().GetSpawnPointById(spawnPointId);
        if (sp != null)
        {
          sp.ForceRelease();
          Debug.Log(
              $"[Launcher] 释放玩家 {otherPlayer.NickName} 占用的生成点 {spawnPointId}"
          );
        }
      }
    }

    // 修改 OnPlayerPropertiesUpdate 以处理生成点状态
    public override void OnPlayerPropertiesUpdate(
        Photon.Realtime.Player targetPlayer,
        Hashtable changedProps
    )
    {
      if (changedProps.ContainsKey(PlayerPropertyKeys.Level))
      {
        Debug.Log(
            $"玩家 {targetPlayer.NickName} 的 PlayerLevel 属性已更新: {changedProps[PlayerPropertyKeys.Level]}"
        );
      }

      if (changedProps.ContainsKey(PlayerPropertyKeys.Ready))
      {
        CheckAllPlayersReady();
      }

      if (changedProps.ContainsKey(PlayerPropertyKeys.Loaded))
      {
        CheckAllPlayersLoaded();
      }

      // 处理生成点属性变化
      if (changedProps.ContainsKey(PlayerPropertyKeys.SpawnPoint))
      {
        object spawnPointValue = changedProps[PlayerPropertyKeys.SpawnPoint];
        Debug.Log($"玩家 {targetPlayer.NickName} 的生成点更新: {spawnPointValue}");
      }
    }

    /// <summary>
    /// 计算生成位置
    /// </summary>
    private Vector3 CalculateSpawnPosition(Transform spawnPoint)
    {
      if (EventChannelLocator.MainContainer.gameSettings.IsStayLobby)
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
      if (!EventChannelLocator.MainContainer.gameSettings.IsStayLobby)
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
        player.transform.LookAt(
            new Vector3(0.182999998f, player.transform.position.y, -0.219999999f)
        );
      }
    }

    /// <summary>
    /// 设置玩家UI
    /// </summary>
    private void SetupPlayerUI(GameObject player)
    {
      localPlayer = PhotonNetwork.LocalPlayer;
      if (string.IsNullOrEmpty(localPlayer.NickName))
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
    private void HideLoading()
    {
      StartCoroutine(Loading.Instance.Hide());
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
      if (isTest)
        return;
      base.OnJoinedRoom();
      // 确保PlayerManager存在且先同步所有玩家数据
      EnsurePlayerManagerExists();

      StartCoroutine(DelayJoinRoom());
    }

    IEnumerator DelayJoinRoom()
    {
      yield return new WaitForSeconds(0.5f);
      CreatedOrJoinedRoom();
    }

    public GameObject GetInactiveObjectByName(string objectName)
    {
      // 只搜索当前场景的层级，避免 Resources.FindObjectsOfTypeAll 遍历所有预制体资源
      foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
      {
        if (root.name == objectName && !root.activeInHierarchy)
          return root;
        var found = FindChildRecursive(root.transform, objectName);
        if (found != null) return found.gameObject;
      }
      return null;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
      for (int i = 0; i < parent.childCount; i++)
      {
        var child = parent.GetChild(i);
        if (child.name == name) return child;
        var result = FindChildRecursive(child, name);
        if (result != null) return result;
      }
      return null;
    }

    private void ApplySavedCharacter()
    {
      int savedIndex = SaveManager.SelectedCharacterIndex;
      switch (savedIndex)
      {
        case CharactorIndex.BingNv:
          currentlySelectedCharacter.name = CharactorName.BingNv;
          break;
        case CharactorIndex.WiZardBoy:
        default:
          currentlySelectedCharacter.name = CharactorName.WiZardBoy;
          break;
      }
    }

    // 切换角色示例
    public void SwitchCharacter(string newCharacterName)
    {
      this.currentlySelectedCharacter.name = newCharacterName;
      RespawnCharacter();
    }

    public void ResetSpawnPointState(int spawnPointIndex)
    {
      foreach (var spawnPoint in ServiceLocator.Get<GameManager>().spawnPoints)
      {
        if (spawnPoint.GetComponent<ISpawnPoint>().Id == spawnPointIndex) { }
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
        if (
            !player.CustomProperties.ContainsKey(PlayerPropertyKeys.Loaded)
            || (bool)player.CustomProperties[PlayerPropertyKeys.Loaded] == false
        )
        {
          Debug.Log($"等待玩家 {player.NickName} 加载场景...");
          return;
        }
      }

      // StartCoroutine(Loading.Instance.Hide());

      allPlayersLoaded = true;
      Debug.Log("所有玩家已加载完成，开始游戏！");
    }

    /// <summary>
    /// 返回大厅（保持房间连接）
    /// </summary>
    public void ReturnToLobby()
    {
      if (!PhotonNetwork.InRoom)
      {
        Debug.LogWarning("[Launcher] 不在房间中，无法返回大厅");
        return;
      }

      // ★★★ 修复：无论房主还是非房主，都先显示 loading ★★★
      // 非房主通过 Photon 场景同步加载大厅后，OnSceneLoaded → Hide() 会自动关闭
      StartCoroutine(ReturnToLobbyCoroutine());
    }

    private IEnumerator ReturnToLobbyCoroutine()
    {
      yield return StartCoroutine(Loading.Instance.Show());

      if (PhotonNetwork.IsMasterClient)
      {
        PhotonNetwork.LoadLevel(1);
        Debug.Log("[Launcher] 房主发起切换到大厅场景");
      }
      else
      {
        Debug.Log("[Launcher] 等待房主同步场景...");
      }
    }

    void OnDestroy()
    {
      // 清理生成点占用
      if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
      {
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PlayerPropertyKeys.SpawnPoint))
        {
          int spawnPointId = (int)
              PhotonNetwork.LocalPlayer.CustomProperties[PlayerPropertyKeys.SpawnPoint];
          ISpawnPoint sp = ServiceLocator.Get<GameManager>().GetSpawnPointById(spawnPointId);
          if (sp != null && (sp as MonoBehaviour) != null)
          {
            sp.ForceRelease();
          }
        }
      }
    }
  }
}