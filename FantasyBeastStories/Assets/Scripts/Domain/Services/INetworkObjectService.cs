using UnityEngine;

namespace Domain.Services
{
    public enum NetworkTarget
    {
        All,
        Others,
        MasterClient,
        AllBuffered
    }

    public interface INetworkObjectService
    {
        GameObject Instantiate(string prefabName, Vector3 position, Quaternion rotation);
        GameObject InstantiateRoomObject(string prefabName, Vector3 position, Quaternion rotation);
        void InvokeRPC(MonoBehaviour source, string methodName, NetworkTarget target, params object[] parameters);
        int GetViewID(Component component);
        int GetViewID(GameObject gameObject);
        GameObject FindByViewID(int viewId);
        void EnsureView(MonoBehaviour source);
        string GetOwnerNickname(MonoBehaviour source);
        int GetOwnerActorNumber(Component source);
    }
}