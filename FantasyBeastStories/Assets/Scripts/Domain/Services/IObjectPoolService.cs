using UnityEngine;

namespace Domain.Services
{
    public interface IObjectPoolService
    {
        GameObject GetInactiveObjectByName(string objectName);
        void ReturnToLobby();
    }
}