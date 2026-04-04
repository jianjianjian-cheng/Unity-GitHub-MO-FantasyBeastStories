using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WordlSpaceUI : MonoBehaviourPunCallbacks
{
    [SerializeField] public Image isMineIcon;
    [SerializeField] public Text playerName;
    [SerializeField] public Text isReadyIcon;
    void Update()
    {
        if (photonView.IsMine)
        {
            isMineIcon.gameObject.SetActive(true);
            playerName.text = " ";
        }
        else
        {
            isMineIcon.gameObject.SetActive(false);
            playerName.text = photonView.Owner.NickName;
        }
        //名称一直朝向摄像机
        Vector3 direction = transform.position - Camera.main.transform.position;
        transform.rotation = Quaternion.LookRotation(direction);
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex > 1)
        {
            isReadyIcon.gameObject.SetActive(false);
            isMineIcon.gameObject.SetActive(false);
        }
    }


    public void UpDatePlayerName(string name)
    {
        if (playerName == null) return;
        if (photonView.IsMine)
        {
            playerName.text = " ";
        }
        else
        {
            playerName.text = name;
        }
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (targetPlayer == photonView.Owner)
        {
            if (changedProps.ContainsKey("PlayerName"))
            {
                string newName = (string)changedProps["PlayerName"];
                UpDatePlayerName(newName);
            }
            if (changedProps.ContainsKey("PlayerReady"))
            {
                bool isReady = (bool)changedProps["PlayerReady"];
                isReadyIcon.text = isReady ? "已就绪" : "未就绪";
            }
        }
    }
}
