using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerStateSync : MonoBehaviourPun, IPunObservable
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    private bool networkFlipX;

    void Awake()
    {
        // 在连接前设置
        PhotonNetwork.SendRate = 30;           // 每秒发送次数（推荐20-30）
        PhotonNetwork.SerializationRate = 30;  // 每秒序列化次数（与SendRate一致）
    }
    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine)
        {
            return;
        }
        else
        {
            // 应用网络状态
            spriteRenderer.flipX = networkFlipX;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 发送玩家状态
            stream.SendNext(spriteRenderer.flipX);
        }
        else
        {
            // 接收玩家状态
            networkFlipX = (bool)stream.ReceiveNext();
        }
    }
}
