using Controllers.Character;
using Core;
using Core.Contracts;
using Core.Network;
using Controllers.Network;
using UnityEngine;
using Core;
using Core.SharedModel;
using Controllers.Experience;

namespace Controllers.Battle
{
  public class Skeleton : AttackableEnemy
  {
    protected override void Awake()
    {
      base.Awake();
    }

    protected override void DropExperience()
    {
      base.DropExperience();
      Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;

      // 经验值从 SO 配置读取
      int expValue = GetExpValue();

      bool isTest = EventChannelLocator.MainContainer.gameSettings.IsTest;
      if (isTest)
      {
        // 测试模式：直接本地生成
        ExperienceManager.HandleSpawnExpBallRPC(0, spawnPosition, expValue);
        return;
      }

      // 联机模式：仅房主生成 ballId 并广播 RPC 到所有客户端
      if (NetworkServiceLocator.PlayerService.IsMasterClient)
      {
        uint ballId = ServiceLocator.Get<ExperienceManager>().GenerateBallId();
        NetworkServiceLocator.ObjectService.InvokeRPC(
            ManagerRpcBridge.Instance, "RPC_SpawnExpBall",
            NetworkTarget.All, (int)ballId, spawnPosition, expValue);
      }
    }

    protected override string GetPoolName() => PoolConst.Skeleton;
  }
}