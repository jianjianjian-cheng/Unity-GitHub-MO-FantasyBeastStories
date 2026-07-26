using UnityEngine;
using NetworkTarget = Controllers.Network.NetworkTarget;

namespace Core.Contracts
{
    public interface INetworkObjectService
    {
        GameObject Instantiate(string prefabName, Vector3 position, Quaternion rotation);
        GameObject InstantiateRoomObject(string prefabName, Vector3 position, Quaternion rotation);
        void InvokeRPC(MonoBehaviour source, string methodName, NetworkTarget target, params object[] parameters);
        int GetViewID(Component component);
        int GetViewID(GameObject gameObject);
        GameObject FindByViewID(int viewId);
        string GetOwnerNickname(MonoBehaviour source);
        int GetOwnerActorNumber(Component source);
    }
}