using System.Collections;
using System.Collections.Generic;
using Domain.Enemy;
using Infrastructure.FX.FireBall;
using Domain.Manager;
using Domain.Event;
using UnityEngine;

namespace Domain.Combat.Trigger
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
            Vector3 hitPosition = other.ClosestPoint(transform.position);
            EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                PoolOperationData.CreateGet("FireBallHitEffectPool", hitPosition, null));
            EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                PoolOperationData.CreateReturn("FireBallPool", gameObject.transform.parent.gameObject));
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
