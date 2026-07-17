using UnityEngine;

namespace Controllers.Services
{
    public interface IObjectPoolService
    {
        GameObject GetInactiveObjectByName(string objectName);
        void ReturnToLobby();
    }
}