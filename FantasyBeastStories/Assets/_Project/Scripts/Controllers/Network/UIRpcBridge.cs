using Controllers.Services;
using Photon.Pun;
using UnityEngine;

namespace Controllers.Network
{
    /// <summary>
    /// Presentation 层 RPC 桥接器（Infrastructure 层）
    /// 统一持有所有 Presentation 层的 [PunRPC] 方法，通过公共方法委托回 Presentation 对象
    /// 职责：纯粹的 RPC 转发，不包含业务逻辑
    /// </summary>
    public class UIRpcBridge : MonoBehaviourPun
    {
        public static UIRpcBridge Instance { get; private set; }

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ============================================================
        // SpawnPoint RPC
        // ============================================================

        [PunRPC]
        public void RPC_UpdateSpawnPointState(int viewID, bool newIsEmpty, int newOccupiedBy)
        {
            var go = NetworkServiceLocator.ObjectService.FindByViewID(viewID);
            if (go != null)
            {
                var spawnPoint = go.GetComponent<UI.Other.SpawnPoint>();
                if (spawnPoint != null)
                {
                    spawnPoint.HandleUpdateSpawnPointState(newIsEmpty, newOccupiedBy);
                }
            }
        }
    }
}