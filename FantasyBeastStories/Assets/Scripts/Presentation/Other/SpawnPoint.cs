using System.Collections;
using System.Collections.Generic;
using Domain.Character;
using Domain.Event;
using Infrastructure.Network;
using Domain.Manager;
using Photon.Pun;
using UnityEngine;
using Application;

namespace Presentation.Other
{
    public class SpawnPoint : MonoBehaviourPun, IPunObservable
    {
        [SerializeField]
        public int Id;

        [SerializeField]
        private bool isEmpty = true;

        [SerializeField]
        private GameObject spawnFx;

        // 记录占用此生成点的玩家 ActorNumber
        private int occupiedByPlayer = -1;

        // 同步变量
        private bool networkIsEmpty = true;
        private int networkOccupiedBy = -1;

        void Start()
        {
            // 初始化时确保所有生成点都为空
            if (PhotonNetwork.IsMasterClient)
            {
                isEmpty = true;
                occupiedByPlayer = -1;
                photonView.RPC("RPC_UpdateSpawnPointState", RpcTarget.All, true, -1);
            }
            InitializeSpawnPoint();
            transform.LookAt(new Vector3(0.182999998f, transform.position.y, -0.219999999f));
        }

        private void InitializeSpawnPoint()
        {
            if (Launcher.instance == null)
            {
                Debug.LogError($"[SpawnPoint-{Id}] Launcher.instance 为 null，请检查场景中 Launcher 脚本的组件引用是否因 namespace 变更而丢失");
                return;
            }

            spawnFx = Launcher.instance.GetInactiveObjectByName("SpawnFX" + Id);
            if (spawnFx == null)
            {
                Debug.Log($"未找到生成点的特效: SpawnFX{Id}");
            }
        }

        IEnumerator PlayFxCoroutine()
        {
            PlayFx();
            yield return new WaitForSeconds(1.5f);
            StopFx();
        }

        private void PlayFx()
        {
            if (spawnFx != null)
            {
                spawnFx.SetActive(true);
            }
        }

        private void StopFx()
        {
            if (spawnFx != null)
            {
                spawnFx.SetActive(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.CompareTag("Player"))
                return;

            PhotonView playerPhotonView = other.GetComponent<PhotonView>();
            if (playerPhotonView == null || !playerPhotonView.IsMine)
                return;

            Debug.Log($"玩家进入生成点: {gameObject.name}，ID: {Id}");
            StartCoroutine(PlayFxCoroutine());

            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.spawnPointIndex = Id;
            }

            // 只有本地玩家才触发占用
            int playerActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            SetOccupied(true, playerActorNumber);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.gameObject.CompareTag("Player"))
                return;

            PhotonView playerPhotonView = other.GetComponent<PhotonView>();
            if (playerPhotonView == null || !playerPhotonView.IsMine)
                return;

            Debug.Log($"玩家离开生成点: {gameObject.name}，ID: {Id}");

            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.spawnPointIndex = -1;
            }

            // 只有当前占用者离开时才释放
            if (occupiedByPlayer == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                SetOccupied(false, -1);
            }
        }

        private void SetOccupied(bool occupied, int playerActorNumber)
        {
            if (EventChannelLocator.MainContainer.gameSettings.IsTest)
                return;
            if (isEmpty == !occupied && occupiedByPlayer == playerActorNumber)
                return;

            isEmpty = !occupied;
            occupiedByPlayer = playerActorNumber;

            Debug.Log(
                $"[SpawnPoint {Id}] 设置占用状态: isEmpty={isEmpty}, occupiedBy={occupiedByPlayer}"
            );

            // 通过 RPC 同步状态
            photonView.RPC("RPC_UpdateSpawnPointState", RpcTarget.All, isEmpty, occupiedByPlayer);

            // 更新玩家属性
            if (occupied)
            {
                UpdatePlayerSpawnPointProperty(Id);
            }
            else if (PhotonNetwork.LocalPlayer.ActorNumber == playerActorNumber)
            {
                ClearPlayerSpawnPointProperty();
            }
        }

        [PunRPC]
        public void RPC_UpdateSpawnPointState(bool newIsEmpty, int newOccupiedBy)
        {
            isEmpty = newIsEmpty;
            occupiedByPlayer = newOccupiedBy;
            networkIsEmpty = newIsEmpty;
            networkOccupiedBy = newOccupiedBy;

            Debug.Log(
                $"[SpawnPoint {Id}] RPC更新状态: isEmpty={isEmpty}, occupiedBy={occupiedByPlayer}"
            );

            // 通知 GameManager 更新生成点列表
            if (GameManager.instance != null)
            {
                // GameManager.instance.OnSpawnPointStateChanged();
            }
        }

        private void UpdatePlayerSpawnPointProperty(int spawnPointId)
        {
            var props = new ExitGames.Client.Photon.Hashtable
            {
                { "CurrentSpawnPoint", spawnPointId },
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        private void ClearPlayerSpawnPointProperty()
        {
            var props = new ExitGames.Client.Photon.Hashtable { { "CurrentSpawnPoint", null } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        public bool IsEmpty()
        {
            return isEmpty;
        }

        public int GetOccupiedByPlayer()
        {
            return occupiedByPlayer;
        }

        // 强制释放生成点（用于玩家退出时）
        public void ForceRelease()
        {
            SetOccupied(false, -1);
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(isEmpty);
                stream.SendNext(occupiedByPlayer);
            }
            else
            {
                isEmpty = (bool)stream.ReceiveNext();
                occupiedByPlayer = (int)stream.ReceiveNext();
            }
        }
    }
}
