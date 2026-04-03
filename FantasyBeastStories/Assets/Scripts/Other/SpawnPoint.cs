using Photon.Pun;
using UnityEngine;

namespace Other
{
    public class SpawnPoint : MonoBehaviourPun, IPunObservable
    {
        [SerializeField] private int Id;
        public bool isEmpty = true;
        private bool isLocalChanged = false;  // 标记是否本地修改

        void Start()
        {
            transform.LookAt(new Vector3(0.182999998f, transform.position.y, -0.219999999f));
        }
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log($"玩家进入生成点: {gameObject.name}，ID: {Id}");
                SetEmpty(false);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log($"玩家离开生成点: {gameObject.name}，ID: {Id}");
                SetEmpty(true);
            }
        }

        private void SetEmpty(bool value)
        {
            if (isEmpty == value) return;

            isEmpty = value;
            isLocalChanged = true;

            // 通知其他客户端
            photonView.RPC("RPC_SetEmpty", RpcTarget.Others, isEmpty);
        }

        [PunRPC]
        private void RPC_SetEmpty(bool value)
        {
            isEmpty = value;
            Debug.Log($"同步: {gameObject.name} isEmpty = {isEmpty}");
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // 只发送本地修改的状态
                if (isLocalChanged)
                {
                    stream.SendNext(isEmpty);
                    isLocalChanged = false;
                }
                else
                {
                    stream.SendNext(null);
                }
            }
            else
            {
                // 接收时只在有数据时更新
                object data = stream.ReceiveNext();
                if (data != null)
                {
                    isEmpty = (bool)data;
                }
            }
        }
    }
}