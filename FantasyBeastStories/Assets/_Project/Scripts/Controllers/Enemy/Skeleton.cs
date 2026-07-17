using Controllers.Character;
using Core;
using Controllers.Services;
using NetworkTarget = Controllers.Services.NetworkTarget;
using Controllers.Network;
using UnityEngine;
using Core;
using Managers;

namespace Controllers.Enemy
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

      // 计算经验值（与 ExperienceBall_Blue.Start 中原有的随机值一致）
      int expValue = Random.Range(50, 71);

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
        uint ballId = ExperienceManager.Instance.GenerateBallId();
        NetworkServiceLocator.ObjectService.InvokeRPC(
            AppRpcBridge.Instance, "RPC_SpawnExpBall",
            NetworkTarget.All, (int)ballId, spawnPosition, expValue);
      }
    }

    protected override string GetPoolName() => PoolConst.Skeleton;
  }
}