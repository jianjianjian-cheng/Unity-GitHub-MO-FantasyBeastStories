using System.Collections;
using System.Collections.Generic;
using Enemies;
using FX;
using Manager;
using Trigger;
using UnityEngine;

namespace Trigger
{
    public class FlyBallTrigger : TriggerBase
    {
        protected FireBallBase ballBase;
        public override void Start()
        {
            ballBase = GetComponentInParent<FireBallBase>();
        }
        public override void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Enemy") || other.GetComponent<EnemyBase>().GetIsDie())
            {
                return;
            }
            base.OnTriggerEnter(other);
            if (other.CompareTag("Enemy"))
                if (ballBase != null)
                {
                    ballBase.HandleEnemyCollisionEnter(other);
                }
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
            if (other.CompareTag("Enemy"))
            {
                if (ballBase != null)
                {
                    ballBase.HandleEnemyCollisionStay(other);
                }
            }
        }
        public override void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Enemy") || other.GetComponent<EnemyBase>().GetIsDie())
            {
                return;
            }
            base.OnTriggerExit(other);
            if (other.CompareTag("Enemy"))
            {
                if (ballBase != null)
                {
                    ballBase.HandleEnemyCollisionExit(other);
                }
            }
        }

    }
}
