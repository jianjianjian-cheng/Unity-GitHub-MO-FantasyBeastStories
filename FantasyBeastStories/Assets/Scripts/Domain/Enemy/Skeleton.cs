using Domain.Character.Attribute;
using Domain.Manager;
using Domain.Pool;
using UnityEngine;
using Domain.Event;

namespace Domain.Enemy
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
      EventChannelLocator.MainContainer.poolOperationChannel.Raise(
          PoolOperationData.CreateSpawn(PoolConst.ExperienceBall_Blue, spawnPosition, Quaternion.identity, null));
    }

    protected override string GetPoolName() => PoolConst.Skeleton;
  }
}
