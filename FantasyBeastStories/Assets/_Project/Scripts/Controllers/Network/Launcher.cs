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

    private bool isTest;

    // 拆分后的逻辑管理器
    private SpawnPointManager _spawnPointManager;
    private NetworkSceneFlow _sceneFlow;

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

      _sceneFlow = new NetworkSceneFlow(this);
      _spawnPointManager = new SpawnPointManager(this, currentlySelectedCharacter);

      // 只在大厅场景（场景索引1）初始化UI
      if (SceneManager.GetActiveScene().buildIndex == 1)
      {
        InitGetUI();
      }
    }
    #endregion

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

    // ==================== 大厅 UI ====================

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

    public void CloseJoinRoomPanel()
    {
      if (TBGC != null) TBGC.SetActive(false);
      if (joinRoomButton != null) joinRoomButton.SetActive(false);
      if (joinRoomInput != null) joinRoomInput.SetActive(false);
    }

    private void InitGetUI()
    {
      Debug.LogWarning("开始初始化UI组件---Launcher");
      nameUI = GameObject.Find("NameUI")?.GetComponent<InputField>();
      TBGC = GetInactiveObjectByName("TBGC");
      joinRoomButton = GetInactiveObjectByName("JoinRoomButton");
      joinRoomInput = GetInactiveObjectByName("JoinRoomInput");
      joinRoom = GameObject.Find("JoinRoom");

      // UI 初始化完成后注入 nameUI 到 SpawnPointManager
      _spawnPointManager.SetNameUI(nameUI);

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

      if (TBGC != null)
      {
        Button closeBtn = TBGC.GetComponentInChildren<Button>(true);
        if (closeBtn != null && closeBtn.gameObject != joinRoomButton)
        {
          closeBtn.onClick.AddListener(CloseJoinRoomPanel);
        }
      }
    }

    public GameObject GetInactiveObjectByName(string objectName)
    {
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

    // ==================== 连接 & 房间管理 ====================

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

    private void SetDefaultName()
    {
      if (string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.NickName))
      {
        PhotonNetwork.LocalPlayer.NickName = "玩家" + PhotonNetwork.LocalPlayer.UserId;
      }
    }

    public override void OnConnectedToMaster()
    {
      if (isTest || SceneManager.GetActiveScene().buildIndex < 1)
        return;
      base.OnConnectedToMaster();
      PhotonNetwork.AutomaticallySyncScene = true;
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

    public override void OnCreatedRoom()
    {
      if (isTest)
        return;
      base.OnCreatedRoom();
      Debug.Log("Created room: " + PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnLeftRoom()
    {
      Debug.Log("已离开房间");
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
    /// 退出房间并返回开始界面
    /// </summary>
    public void QuitToMainMenu()
    {
      if (PhotonNetwork.InRoom)
      {
        PhotonNetwork.AutomaticallySyncScene = false;
        _spawnPointManager.ClearLocalPlayerSpawnPoint();
        isQuittingToMenu = true;
        isAutoCreate = false;
        PhotonNetwork.LeaveRoom();
        Debug.Log("正在退出房间返回主菜单...");
      }
      else
      {
        StartCoroutine(ShowAndLoadMainMenu());
      }
    }

    private IEnumerator ShowAndLoadMainMenu()
    {
      yield return StartCoroutine(Loading.Instance.Show());
      LoadMainMenuScene();
    }

    private void LoadMainMenuScene()
    {
      isQuittingToMenu = false;
      pendingRoomName = "";
      isJoiningRoom = false;
      isAutoCreate = false;
      PhotonNetwork.AutomaticallySyncScene = false;
      SceneManager.LoadScene(0);
    }

    // ==================== PUN 回调路由 ====================

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

    public override void OnJoinedRoom()
    {
      Debug.Log("Joined room: " + PhotonNetwork.CurrentRoom.Name);
      if (isTest)
        return;
      base.OnJoinedRoom();
      _spawnPointManager.EnsurePlayerManagerExists();
      StartCoroutine(DelayJoinRoom());
    }

    IEnumerator DelayJoinRoom()
    {
      yield return new WaitForSeconds(0.5f);
      _spawnPointManager.CreatedOrJoinedRoom();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
      if (scene.buildIndex == 1)
      {
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
        _sceneFlow.ResetForLobby();
        _sceneFlow.ResetLocalReady();

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
          StartCoroutine(_spawnPointManager.SpawnPlayerAfterDelay());
        }

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

        Hashtable loadProps = new Hashtable
            {
                { PlayerPropertyKeys.Level, true },
                { PlayerPropertyKeys.Loaded, true },
                { PlayerPropertyKeys.PlayerName, PhotonNetwork.LocalPlayer.NickName },
            };

        PhotonNetwork.LocalPlayer.SetCustomProperties(loadProps);
        Debug.Log(
            $"[Launcher] 本地玩家场景加载完成，设置 PlayerLoaded=true: {PhotonNetwork.LocalPlayer.NickName}"
        );

        _spawnPointManager.CreatedOrJoinedRoom();
      }
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
      _spawnPointManager.HandlePlayerLeftRoom(otherPlayer);
    }

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
        _sceneFlow.CheckAllPlayersReady();
      }

      if (changedProps.ContainsKey(PlayerPropertyKeys.Loaded))
      {
        _sceneFlow.CheckAllPlayersLoaded();
      }

      if (changedProps.ContainsKey(PlayerPropertyKeys.SpawnPoint))
      {
        object spawnPointValue = changedProps[PlayerPropertyKeys.SpawnPoint];
        Debug.Log($"玩家 {targetPlayer.NickName} 的生成点更新: {spawnPointValue}");
      }
    }

    void OnDestroy()
    {
      _spawnPointManager.HandleDestroy();
    }

    // ==================== 接口门面（IGameActionService / IObjectPoolService） ====================

    // IGameActionService
    public void SetLocalReady(bool ready) => _sceneFlow.SetLocalReady(ready);

    // IObjectPoolService
    public void ReturnToLobby() => _sceneFlow.ReturnToLobby();

    // CharactorPanel 直接调用
    public void SwitchCharacter(string newCharacterName) => _spawnPointManager.SwitchCharacter(newCharacterName);
  }
}
