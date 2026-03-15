using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Cinemachine;
using Player;
using Charactors;

public class LocalCameraActivator : MonoBehaviourPun
{
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    private GameObject parentPlayer = null;
    // Start is called before the first frame update
    void Start()
    {
        // Ensure we have a reference to the player root even for remote instances.
        parentPlayer = transform.parent?.Find("Player")?.gameObject;

        if (parentPlayer == null)
        {
            Debug.LogWarning("LocalCameraActivator: could not find Player child under parent.");
            return;
        }

        if (photonView.IsMine)
        {
            virtualCamera.gameObject.SetActive(true);
            parentPlayer.GetComponent<PlayerController>().enabled = true;
        }
        else
        {
            virtualCamera.gameObject.SetActive(false);
            parentPlayer.GetComponent<PlayerController>().enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
