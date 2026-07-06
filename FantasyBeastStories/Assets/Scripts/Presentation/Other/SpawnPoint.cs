using System.Collections;
using System.Collections.Generic;
using Domain.Character;
using Domain.Event;
using Domain.Services;
using UnityEngine;
using Application;
using Infrastructure.Network;
using Photon.Pun;

namespace Presentation.Other
{
    public class SpawnPoint : MonoBehaviour, ISpawnPoint, IPunObservable
    {
        [SerializeField]
        private int id;
        public int Id { get => id; set => id = value; }

        [SerializeField]
        private bool isEmpty = true;

        [SerializeField]
        private GameObject spawnFx;

        // 记录占用此生成点的玩家 ActorNumber
        private int occupiedByPlayer = -1;

        void Start()
        {
            // 初始化时确保所有生成点都为空
            if (NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                isEmpty = true;
                occupiedByPlayer = -1;
                var viewID = NetworkServiceLocator.ObjectService.GetViewID(gameObject);
                NetworkServiceLocator.ObjectService.InvokeRPC(PresentationRpcBridge.Instance, "RPC_UpdateSpawnPointState", NetworkTarget.All, viewID, true, -1);
            }
            InitializeSpawnPoint();
            transform.LookAt(new Vector3(0.182999998f, transform.position.y, -0.219999999f));
        }

        private void InitializeSpawnPoint()
        {
            if (NetworkServiceLocator.ObjectPoolService == null)
            {
                Debug.LogError($"[SpawnPoint-{Id}] ObjectPoolService 未注册");
                return;
            }

            spawnFx = NetworkServiceLocator.ObjectPoolService.GetInactiveObjectByName("SpawnFX" + Id);
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

            if (!NetworkServiceLocator.PlayerService.IsOwnerOf(other.gameObject))
                return;

            Debug.Log($"玩家进入生成点: {gameObject.name}，ID: {Id}");
            StartCoroutine(PlayFxCoroutine());

            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.spawnPointIndex = Id;
            }

            // 只有本地玩家才触发占用
            int playerActorNumber = NetworkServiceLocator.PlayerService.GetLocalActorNumber();
            SetOccupied(true, playerActorNumber);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.gameObject.CompareTag("Player"))
                return;

            if (!NetworkServiceLocator.PlayerService.IsOwnerOf(other.gameObject))
                return;

            Debug.Log($"玩家离开生成点: {gameObject.name}，ID: {Id}");

            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.spawnPointIndex = -1;
            }

            // 只有当前占用者离开时才释放
            if (occupiedByPlayer == NetworkServiceLocator.PlayerService.GetLocalActorNumber())
            {
                SetOccupied(false, -1);
            }
        }

        public void SetOccupied(bool occupied, int playerActorNumber)
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
            var viewID = NetworkServiceLocator.ObjectService.GetViewID(gameObject);
            NetworkServiceLocator.ObjectService.InvokeRPC(PresentationRpcBridge.Instance, "RPC_UpdateSpawnPointState", NetworkTarget.All, viewID, isEmpty, occupiedByPlayer);

            // 更新玩家属性
            if (occupied)
            {
                UpdatePlayerSpawnPointProperty(Id);
            }
            else if (NetworkServiceLocator.PlayerService.GetLocalActorNumber() == playerActorNumber)
            {
                ClearPlayerSpawnPointProperty();
            }
        }

        /// <summary>
        /// 由 PresentationRpcBridge.RPC_UpdateSpawnPointState 调用
        /// </summary>
        public void HandleUpdateSpawnPointState(bool newIsEmpty, int newOccupiedBy)
        {
            isEmpty = newIsEmpty;
            occupiedByPlayer = newOccupiedBy;

            Debug.Log(
                $"[SpawnPoint {Id}] RPC更新状态: isEmpty={isEmpty}, occupiedBy={occupiedByPlayer}"
            );
        }

        private void UpdatePlayerSpawnPointProperty(int spawnPointId)
        {
            NetworkServiceLocator.PlayerService.SetCustomProperty("CurrentSpawnPoint", spawnPointId);
        }

        private void ClearPlayerSpawnPointProperty()
        {
            NetworkServiceLocator.PlayerService.SetCustomProperty("CurrentSpawnPoint", null);
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

        /// <summary>
        /// IPunObservable 接口实现（空实现）
        /// 本组件通过 RPC 同步状态，不需要 PhotonView 序列化数据。
        /// 但 PhotonView 的 Observed Components 列表中挂载了此组件，
        /// 因此必须实现此接口以避免运行时错误。
        /// </summary>
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            // 所有状态同步通过 RPC 完成，无需序列化
        }
    }
}