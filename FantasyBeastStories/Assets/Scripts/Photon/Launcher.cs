using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Manager;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.VisualScripting;

public class Launcher : MonoBehaviourPunCallbacks
{
    private bool isAutoCreate = false;
    private bool isJoiningRoom = false;
    [SerializeField] public GameObject currentlySelectedCharacter;
    private Photon.Realtime.Player localPlayer;
    private InputField nameUI;
    private Button joinRoom;
    private GameObject TBGC;
    private GameObject joinRoomButton;
    private GameObject joinRoomInput;
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
    private bool isTest; // 是否测试模式
    // Start is called before the first frame update
    void Start()
    {
        isTest = GameManager.instance != null && GameManager.isTest;
        if (isTest) return;
        Application.runInBackground = true;
        PhotonNetwork.ConnectUsingSettings();
        Initialize();
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

    //如果点击到TBGC以外的区域，关闭输入框
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TBGC.SetActive(false);
            joinRoomButton.SetActive(false);
            joinRoomInput.SetActive(false);
        }
    }

    private string pendingRoomName = "";

    public void SwitchRoom(string newRoomName)
    {
        if (PhotonNetwork.InRoom)
        {
            pendingRoomName = newRoomName;
            isJoiningRoom = true;  // 设置标志，在 OnConnectedToMaster 中处理加入房间
            PhotonNetwork.LeaveRoom();  // 离开房间，等待 OnLeftRoom 回调
            Debug.Log("正在离开当前房间...");
        }
    }

    // 离开房间完成回调
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
            Initialize();
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
        Debug.Log("执行CreatedOrJoinedRoom");
        GameObject player = PhotonNetwork.Instantiate("WizardBoyRoot", new Vector3(80, 2, 80), Quaternion.identity);
        player.name = "Player" + PhotonNetwork.LocalPlayer.UserId;
        Transform spawnPoint = GameManager.instance.GetEmptySpawnPoint().transform;
        if (spawnPoint != null)
        {
            player.transform.position = spawnPoint.position;
            player.transform.LookAt(new Vector3(0.182999998f, player.transform.position.y, -0.219999999f)); // 朝向中心点
        }
        if (LoadingCanvas.instance != null)
        {
            LoadingCanvas.instance.HideLoading();
            LoadingCanvas.instance.GetComponentInChildren<Animator>().SetBool("FadeIn", false);
        }
        if (GameObject.Find("RoomName") != null)
        {
            string roomName = PhotonNetwork.CurrentRoom.Name;
            GameObject.Find("RoomName").GetComponent<Text>().text = roomName;
            localPlayer = PhotonNetwork.LocalPlayer;
            localPlayer.NickName = "玩家" + localPlayer.UserId;
            nameUI.text = localPlayer.NickName;
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room: " + PhotonNetwork.CurrentRoom.Name);
        if (isTest) return;
        base.OnJoinedRoom();
        CreatedOrJoinedRoom();
    }

    GameObject GetInactiveObjectByName(string objectName)
    {
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        return System.Array.Find(allObjects, obj => obj.name == objectName && !obj.activeInHierarchy);
    }
}
