using System.Collections;
using System.Collections.Generic;
using Atttibute;
using Charactors;
using Events;
using Manager;
using Trigger;
using UnityEngine;

namespace FX
{
    public class ImpactCannon : TriggerBase
    {
        [SerializeField] private bool isTest;
        private int activeCount = 0;
        private AttributePlayerBase attributePlayerBase;
        private float Speed = 15f;
        private Rigidbody rb;
        // Start is called before the first frame update

        void Awake()
        {
            isTest = GameManager.instance != null && GameManager.isTest;
            rb = GetComponent<Rigidbody>();
        }
        public void OnEnable()
        {
            Invoke("DelayDestorySelf", 0.5f);
        }

        void OnDisable()
        {
            Debug.Log("冲击炮被禁用，返回对象池");
            rb.velocity = Vector3.zero;
        }

        public void StartShoot(Vector3 direction)
        {
            //仅仅保留x和z轴平面所在的方向
            direction.y = 0;
            rb.velocity = direction.normalized * Speed;
        }

        public override void Start()
        {
            base.Start();
        }

        public override void Update()
        {
            base.Update();
        }

        public override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);
            if (activeCount == 0)
            {
                attributePlayerBase = EventManager.instance.GetAttributePlayerBase(EventNames.UpdateAttributePlayer);
            }
            //触发冲击炮击中效果
            if (!other.gameObject.CompareTag("Enemy")) return;
            Debug.Log("触发冲击炮击中效果");
            GameObject gameObject = ObjectPoolManager.instance.GetFromPoolAndActivate("ImpactCannonHitCommonPool", other.ClosestPoint(transform.position));
            if (gameObject != null)
            {
                gameObject.GetComponentInChildren<ParticleSystem>().Play();
            }
            activeCount++;
            //是否暴击
            bool isCritical = Random.Range(0, 1f) <= attributePlayerBase.GetCriticalChance() ? true : false;
            //伤害判定
            DamageEventArgs damageEventArgs = new DamageEventArgs(
                DamageType.Fire,
                gameObject,
                other.gameObject,
                attributePlayerBase.GetAttackPower(),
                isCritical,
                attributePlayerBase.GetCriticalMultiplier()
            );
            //触发伤害事件
            EventManager.instance.TriggerEventComplex(EventNames.DamageReceived, damageEventArgs);
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
            ObjectPoolManager.instance.ReturnToPool(ObjectPoolConst.ImpactCannonTriggerPool, gameObject);
        }
    }
}
