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

public class Launcher : MonoBehaviourPunCallbacks
{
    private bool allPlayersLoaded = false;
    private bool isAutoCreate = false;
    private bool isJoiningRoom = false;
    [SerializeField] public GameObject currentlySelectedCharacter;
    private Photon.Realtime.Player localPlayer;
    private InputField nameUI;
    private Button joinRoom;
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
        TBGC = GameObject.Find("TBGC");
        joinRoomButton = GameObject.Find("JoinRoomButton");
        joinRoomInput = GameObject.Find("JoinRoomInput");
        joinRoom = GameObject.Find("JoinRoom").GetComponent<Button>();
        TBGC = GetInactiveObjectByName("TBGC");
        joinRoomButton = GetInactiveObjectByName("JoinRoomButton");
        joinRoomInput = GetInactiveObjectByName("JoinRoomInput");
        joinRoom.onClick.AddListener(() =>
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
        yield return new WaitForSeconds(1f);

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
        Transform spawnPoint = GameManager.instance.GetEmptySpawnPoint().transform;
        GameObject player = null;
        if (spawnPoint != null)
        {
            // 在生成点周围XZ平面内随机生成
            Vector2 randomCircle = Random.insideUnitCircle * 10.0f;
            Vector3 randomOffset = new Vector3(randomCircle.x, spawnPoint.position.y, randomCircle.y);
            Vector3 spawnPosition = spawnPoint.position + spawnPoint.TransformVector(randomOffset);
            if (GameManager.isStayLobby)
            {
                spawnPosition = spawnPoint.position;
            }
            player = PhotonNetwork.Instantiate("WizardBoyRoot", spawnPosition, spawnPoint.rotation);
            player.transform.rotation = Quaternion.Euler(0, player.transform.rotation.y, 0);
            player.name = "Player" + PhotonNetwork.LocalPlayer.UserId;
            Debug.LogWarning("离开大厅" + GameManager.isStayLobby);
            if (!GameManager.isStayLobby)
            {
                GameObject vcam = player.transform.Find("VirtualCamera").gameObject;
                if (vcam != null)
                {
                    vcam.SetActive(true);
                }
            }
            else
            {
                player.transform.LookAt(new Vector3(0.182999998f, player.transform.position.y, -0.219999999f));
            }
        }
        if (LoadingCanvas.instance != null)
        {
            LoadingCanvas.instance.HideLoading();
        }
        if (GameObject.Find("RoomName") != null)
        {
            string roomName = PhotonNetwork.CurrentRoom.Name;
            GameObject.Find("RoomName").GetComponent<Text>().text = roomName;
            localPlayer = PhotonNetwork.LocalPlayer;
            localPlayer.NickName = "玩家" + localPlayer.UserId;
            nameUI.text = localPlayer.NickName;
        }
        player.GetComponentInChildren<WordlSpaceUI>().UpDatePlayerName(localPlayer.NickName);
    }

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
        return System.Array.Find(allObjects, obj => obj.name == objectName && !obj.activeInHierarchy);
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("PlayerLevel"))
        {
            Debug.Log($"玩家 {targetPlayer.NickName} 的 PlayerLevel 属性已更新: {changedProps["PlayerLevel"]}");
        }

        if (changedProps.ContainsKey("PlayerReady"))
        {
            // 所有玩家都检查准备状态
            CheckAllPlayersReady();
        }

        if (changedProps.ContainsKey("PlayerLoaded"))
        {
            // 所有玩家都检查加载完成状态
            CheckAllPlayersLoaded();
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