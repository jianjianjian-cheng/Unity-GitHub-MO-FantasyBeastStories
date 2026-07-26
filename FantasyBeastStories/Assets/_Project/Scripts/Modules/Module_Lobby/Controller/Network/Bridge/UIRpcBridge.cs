using Core.Contracts;
using Core.Network;
using Photon.Pun;
using UnityEngine;

namespace Controllers.Network
{
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