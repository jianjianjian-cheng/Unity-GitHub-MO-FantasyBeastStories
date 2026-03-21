using System.Collections;
using System.Collections.Generic;
using Manager;
using Trigger;
using UnityEngine;

namespace FX
{
    public class ImpactCannon : TriggerBase
    {
        private float Speed = 15f;
        private Rigidbody rb;
        // Start is called before the first frame update
        public void OnEnable()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.Log("rb为空");
            }
            else
            {
                rb.velocity = transform.forward * Speed;
            }
            Invoke(nameof(DelayDestorySelf), 2f);
        }

        public override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);
        }

        public override void OnTriggerStay(Collider other)
        {
            base.OnTriggerStay(other);
        }

        public override void OnTriggerExit(Collider other)
        {
            base.OnTriggerExit(other);
        }

        //画出自身的范围
        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

        private void DelayDestorySelf()
        {
            ManagerBase.instance.GetComponent<ObjectPoolManager>().ReturnToPool(ObjectPoolConst.ImpactCannonCommonPool, gameObject.transform.parent.gameObject);
        }
    }
}
