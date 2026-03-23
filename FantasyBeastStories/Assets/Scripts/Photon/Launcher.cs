using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Manager;

public class Launcher : MonoBehaviourPunCallbacks
{
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

        PhotonNetwork.JoinOrCreateRoom("Room", new Photon.Realtime.RoomOptions() { MaxPlayers = 4 }, default);
    }

    public override void OnJoinedRoom()
    {
        if (isTest) return;
        base.OnJoinedRoom();
        PhotonNetwork.Instantiate("WizardBoyRoot", new Vector3(80, 2, 80), Quaternion.identity);
    }
}
