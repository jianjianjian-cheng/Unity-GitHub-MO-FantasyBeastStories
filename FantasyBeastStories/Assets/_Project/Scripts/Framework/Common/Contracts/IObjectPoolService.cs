using UnityEngine;

namespace Core.Contracts
{
    public interface IObjectPoolService
    {
        GameObject GetInactiveObjectByName(string objectName);
        void ReturnToLobby();
    }
}