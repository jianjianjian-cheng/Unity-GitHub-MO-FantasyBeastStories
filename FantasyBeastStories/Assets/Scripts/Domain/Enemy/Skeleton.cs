using Domain.Character.Attribute;
using Domain.Manager;
using UnityEngine;
using Infrastructure.Network;
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
                PoolOperationData.CreateSpawn(NetworkObjectPoolConst.ExperienceBall_Blue, spawnPosition, Quaternion.identity, null));
        }

        protected override string GetPoolName() => NetworkObjectPoolConst.Skeleton;
    }
}
