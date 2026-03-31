using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Manager;

public class Launcher : MonoBehaviourPunCallbacks
{
    [SerializeField] public GameObject currrnySlecetedCharacter;
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
    }

    public override void OnConnectedToMaster()
    {
        if (isTest) return;
        base.OnConnectedToMaster();
        Debug.Log("Connected to master");
        PhotonNetwork.CreateRoom("Room", new Photon.Realtime.RoomOptions() { MaxPlayers = 4 }, default);
        // PhotonNetwork.JoinOrCreateRoom("Room", new Photon.Realtime.RoomOptions() { MaxPlayers = 4 }, default);
    }

    public override void OnCreatedRoom()
    {
        if (isTest) return;
        base.OnCreatedRoom();
        GameObject player = PhotonNetwork.Instantiate("WizardBoyRoot", new Vector3(80, 2, 80), Quaternion.identity);
        Transform spawnPoint = GameManager.instance.GetEmptySpawnPoint();
        if (spawnPoint != null)
        {
            player.transform.position = spawnPoint.position;
            player.transform.LookAt(new Vector3(0.182999998f, player.transform.position.y, -0.219999999f)); // 朝向中心点
        }
        LoadingCanvas.instance.HideLoading();
        LoadingCanvas.instance.GetComponentInChildren<Animator>().SetBool("FadeIn", false);
    }

    public override void OnJoinedRoom()
    {
        if (isTest) return;
        base.OnJoinedRoom();
    }
}
