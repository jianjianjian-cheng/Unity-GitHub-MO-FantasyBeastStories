using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Controllers.Network
{
    public class PlayerStateSync : MonoBehaviourPun, IPunObservable
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        private bool networkFlipX;

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
}