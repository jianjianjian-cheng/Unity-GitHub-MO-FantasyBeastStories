using Core;
using Core.Contracts;
using Core.Network;
using NetworkTarget = Controllers.Network.NetworkTarget;
using Controllers.Network;
using UnityEngine;
using Core;
using Managers;

namespace Controllers.Enemy
{
  public class Dragon : AttackableEnemy
  {
    protected override void DropExperience()
    {
      base.DropExperience();
      Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;

      // 经验值从 SO 配置读取
      int expValue = GetExpValue();

      bool isTest = EventChannelLocator.MainContainer.gameSettings.IsTest;
      if (isTest)
      {
        ExperienceManager.HandleSpawnExpBallRPC(0, spawnPosition, expValue);
        return;
      }

      if (NetworkServiceLocator.PlayerService.IsMasterClient)
      {
        uint ballId = ServiceLocator.Get<ExperienceManager>().GenerateBallId();
        NetworkServiceLocator.ObjectService.InvokeRPC(
            ManagerRpcBridge.Instance, "RPC_SpawnExpBall",
            NetworkTarget.All, (int)ballId, spawnPosition, expValue);
      }
    }

    protected override string GetPoolName() => PoolConst.Dragon;
  }
}
