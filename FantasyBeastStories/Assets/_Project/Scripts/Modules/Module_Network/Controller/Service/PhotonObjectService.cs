using Core.Contracts;
using Core.Network;
using Photon.Pun;
using UnityEngine;
using Core.SharedModel;

namespace Controllers.Network
{
    public class PhotonObjectService : INetworkObjectService
    {
        public GameObject Instantiate(string prefabName, Vector3 position, Quaternion rotation)
        {
            return PhotonNetwork.Instantiate(prefabName, position, rotation);
        }

        public GameObject InstantiateRoomObject(string prefabName, Vector3 position, Quaternion rotation)
        {
            return PhotonNetwork.InstantiateRoomObject(prefabName, position, rotation);
        }

        public void InvokeRPC(MonoBehaviour source, string methodName, NetworkTarget target, params object[] parameters)
        {
            PhotonView photonView = source.GetComponent<PhotonView>();
            if (photonView == null)
            {
                Debug.LogWarning($"[PhotonObjectService] {source.name} 上没有 PhotonView 组件，无法调用 RPC {methodName}");
                return;
            }
            photonView.RPC(methodName, NetworkTargetMapper.ToRpcTarget(target), parameters);
        }

        public int GetViewID(Component component)
        {
            PhotonView photonView = component.GetComponent<PhotonView>();
            return photonView != null ? photonView.ViewID : -1;
        }

        public int GetViewID(GameObject gameObject)
        {
            if (gameObject == null) return -1;
            PhotonView photonView = gameObject.GetComponent<PhotonView>();
            return photonView != null ? photonView.ViewID : -1;
        }

        public GameObject FindByViewID(int viewId)
        {
            PhotonView photonView = PhotonView.Find(viewId);
            return photonView != null ? photonView.gameObject : null;
        }

        public string GetOwnerNickname(MonoBehaviour source)
        {
            if (source == null) return "";
            PhotonView pv = source.GetComponentInParent<PhotonView>();
            return pv != null && pv.Owner != null ? pv.Owner.NickName : "";
        }

        public int GetOwnerActorNumber(Component source)
        {
            if (source == null) return -1;
            PhotonView pv = source.GetComponentInParent<PhotonView>();
            return pv != null && pv.Owner != null ? pv.Owner.ActorNumber : -1;
        }
    }
}