using UnityEngine;
using Domain.PowerUp.Data;

namespace Domain.Services
{
    /// <summary>
    /// 道具服务接口 - 遵循依赖倒置原则
    /// </summary>
    public interface IPowerUpService
    {
        void SpawnPowerUp(PowerUpDataSO data, Vector3 position);
        void SpawnRandomPowerUp(Vector3 position);
        int GetActivePowerUpCount();
        void ClearAllPowerUps();
    }
}