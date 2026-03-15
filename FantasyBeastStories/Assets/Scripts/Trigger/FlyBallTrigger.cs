using System.Collections;
using System.Collections.Generic;
using Enemies;
using Manager;
using Trigger;
using UnityEngine;

namespace Trigger
{
    public class FlyBallTrigger : TriggerBase
    {
        public override void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Enemy") || other.GetComponent<EnemyBase>().GetIsDie())
            {
                return;
            }
            base.OnTriggerEnter(other);
            //获取碰撞点
            Vector3 hitPosition = other.ClosestPoint(transform.position);
            // 播放火球击中效果
            ManagerBase.instance.GetComponent<ObjectPoolManager>().GetFromPoolAndActivate("FireBallHitEffectPool", hitPosition);
            ManagerBase.instance.GetComponent<ObjectPoolManager>().ReturnToPool("FireBallPool", gameObject.transform.parent.gameObject);
        }
        public override void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Enemy") || other.GetComponent<EnemyBase>().GetIsDie())
            {
                return;
            }
            base.OnTriggerStay(other);
        }
        public override void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Enemy") || other.GetComponent<EnemyBase>().GetIsDie())
            {
                return;
            }
            base.OnTriggerExit(other);
        }

    }
}
