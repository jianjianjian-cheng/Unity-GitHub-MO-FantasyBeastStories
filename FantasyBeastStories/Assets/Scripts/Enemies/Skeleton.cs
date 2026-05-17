using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Manager;
using UnityEngine;

namespace Enemies
{
    public class Skeleton : AttackableEnemy
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            RegisterDamageEvent();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnregisterDamageEvent();
        }

        protected override void EnterDie()
        {
            base.EnterDie();
        }

        protected override void DropExperience()
        {
            base.DropExperience();
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f; // 调整生成位置，使经验球稍微悬浮在敌人上方
            ObjectPoolManager.instance.GetFromPoolAndActivate(ObjectPoolConst.ExperienceBall_BluePool, spawnPosition);
        }
    }
}