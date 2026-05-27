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
            ObjectPoolManager.instance.GetFromPoolAndActivate(
                ObjectPoolConst.ExperienceBall_BluePool,
                spawnPosition
            );
        }

        // 指定该怪物回收到骷髅池
        protected override string GetPoolName() => MonsterPoolConst.Skeleton;
    }
}
