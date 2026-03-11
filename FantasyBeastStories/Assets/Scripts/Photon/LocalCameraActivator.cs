using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Cinemachine;
using Player;

public class LocalCameraActivator : MonoBehaviourPun
{
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    // Start is called before the first frame update
    void Start()
    {
        if (photonView.IsMine)
        {
            virtualCamera.gameObject.SetActive(true);
            GetComponentInParent<Protagonist>().enabled = true;
        }
        else
        {
            virtualCamera.gameObject.SetActive(false);
            GetComponentInParent<Protagonist>().enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
