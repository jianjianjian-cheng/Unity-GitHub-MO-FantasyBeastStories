using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class GamePauseManager : MonoBehaviourPunCallbacks
{
    public static bool isPaused = false;
    public static GamePauseManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            EnsurePhotonView();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static float DeltaTime
    {
        get { return isPaused ? 0f : Time.deltaTime; }
    }

    public static float FixedDeltaTime
    {
        get { return isPaused ? 0f : Time.fixedDeltaTime; }
    }

    void Update()
    {
        if (PhotonNetwork.IsMasterClient && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        SetPause(!isPaused);
    }

    public void SetPause(bool pause)
    {
        isPaused = pause;

        if (PhotonNetwork.IsMasterClient)
        {
            ExitGames.Client.Photon.Hashtable pauseProps = new ExitGames.Client.Photon.Hashtable();
            pauseProps["IsPaused"] = isPaused;
            PhotonNetwork.CurrentRoom.SetCustomProperties(pauseProps);

            if (photonView != null)
            {
                photonView.RPC("RPC_SetPauseState", RpcTarget.All, isPaused);
            }
        }
    }

    [PunRPC]
    public void RPC_SetPauseState(bool pause)
    {
        isPaused = pause;

        if (pause)
        {
            // 暂停时：冻结所有动画，保持当前帧
            FreezeAllAnimations();
        }
        else
        {
            // 恢复时：恢复所有动画
            ResumeAllAnimations();
        }
    }

    /// <summary>
    /// 冻结所有动画（保持当前帧）
    /// </summary>
    private void FreezeAllAnimations()
    {
        Animator[] allAnimators = FindObjectsOfType<Animator>();
        foreach (Animator animator in allAnimators)
        {
            // 设置速度为0，冻结动画
            animator.speed = 0f;

            // 确保动画保持在当前帧（某些情况下需要强制更新）
            animator.Update(0f);
        }
    }

    /// <summary>
    /// 恢复所有动画
    /// </summary>
    private void ResumeAllAnimations()
    {
        Animator[] allAnimators = FindObjectsOfType<Animator>();
        foreach (Animator animator in allAnimators)
        {
            // 恢复动画速度
            animator.speed = 1f;
        }
    }

    public override void OnRoomPropertiesUpdate(
        ExitGames.Client.Photon.Hashtable propertiesThatChanged
    )
    {
        if (propertiesThatChanged.ContainsKey("IsPaused"))
        {
            bool newPauseState = (bool)propertiesThatChanged["IsPaused"];
            if (isPaused != newPauseState)
            {
                isPaused = newPauseState;
                if (isPaused)
                {
                    FreezeAllAnimations();
                }
                else
                {
                    ResumeAllAnimations();
                }
            }
        }
    }

    private void EnsurePhotonView()
    {
        PhotonView pv = GetComponent<PhotonView>();
        if (pv == null)
        {
            pv = gameObject.AddComponent<PhotonView>();
        }
    }
}
