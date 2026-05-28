using Charactors.Attribute;
using Manager;
using UnityEngine;

namespace Enemies
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
            NetworkObjectPoolManager.instance.Spawn(
                NetworkObjectPoolConst.ExperienceBall_Blue,
                spawnPosition,
                Quaternion.identity
            );
        }

        // 指定该怪物回收到骷髅池
        protected override string GetPoolName() => NetworkObjectPoolConst.Skeleton;
    }
}
