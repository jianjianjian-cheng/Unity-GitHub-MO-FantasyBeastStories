using System.Collections;
using System.Collections.Generic;
using Manager;
using Trigger;
using UnityEngine;

namespace Trigger
{
    public class FlyBallTrigger : TriggerBase
    {
        public override void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Enemy"))
            {
                return;
            }
            ManagerBase.instance.GetComponent<ObjectPoolManager>().ReturnToPool("FireBallPool", gameObject.transform.parent.gameObject);
            // 播放火球击中效果
            ManagerBase.instance.GetComponent<ObjectPoolManager>().GetFromPoolAndActivate("FireBallHitEffectPool", other.gameObject.transform.position);
            base.OnTriggerEnter(other);
        }
        public override void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Enemy"))
            {
                return;
            }
            base.OnTriggerStay(other);
        }
        public override void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Enemy"))
            {
                return;
            }
            base.OnTriggerExit(other);
        }

    }
}
