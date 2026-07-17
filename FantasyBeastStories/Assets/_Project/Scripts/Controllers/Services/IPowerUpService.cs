using UnityEngine;
using Controllers.PowerUp;

namespace Controllers.Services
{
    /// <summary>
    /// 道具服务接口
    /// </summary>
    public interface IPowerUpService
    {
        void SpawnPowerUp(PowerUpDataSO data, Vector3 position);
        void SpawnRandomPowerUp(Vector3 position);
        int GetActivePowerUpCount();
        void ClearAllPowerUps();
    }
}