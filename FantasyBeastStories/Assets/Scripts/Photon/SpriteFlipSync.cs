using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class SpriteFlipSync : MonoBehaviourPun, IPunObservable
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    private bool currentFlipX;
    private bool networkFlipX;
    // Start is called before the first frame update
    void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 本地玩家：发送翻转状态
            stream.SendNext(currentFlipX);
        }
        else
        {
            // 网络玩家：接收翻转状态
            networkFlipX = (bool)stream.ReceiveNext();
        }
    }
}
