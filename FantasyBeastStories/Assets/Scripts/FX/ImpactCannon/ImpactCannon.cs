using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using Atttibute;
using Charactors;
using Events;
using Manager;
using Other;
using Photon.CastSciprt;
using Trigger;
using Unity.VisualScripting;
using UnityEngine;

namespace FX
{
    public class ImpactCannon : TriggerBase
    {
        // true  → 我是发射者，我负责判定伤害
        // false → 我是别人的火球，我只负责视觉表现
        private bool _isMyCast = true;
        private CastNetwork _networkCaster;
        [SerializeField] private bool isTest;
        private int maxAttackCount = 1;
        private int attackCount = 0;
        private AttributePlayerBase attributePlayerBase;
        private float Speed = 15f;
        private Rigidbody rb;
        // Start is called before the first frame update

        void Awake()
        {
            isTest = GameManager.instance != null && GameManager.isTest;
            rb = GetComponent<Rigidbody>();
            _networkCaster = FindObjectOfType<CastNetwork>();
        }
        public void OnEnable()
        {
            attackCount = 0;
            Invoke("DelayDestorySelf", 0.5f);
        }

        void OnDisable()
        {
            Debug.Log("冲击炮被禁用，返回对象池");
            rb.velocity = Vector3.zero;
            CancelInvoke();
        }

        public void GetAttributeFromPlayer(AttributePlayerBase attributePlayerBase)
        {
            this.attributePlayerBase = attributePlayerBase;
        }

        /// <summary>
        /// 发射物体
        /// </summary>
        /// <param name="direction">发射方向</param>
        /// <param name="isMine">是否由本地炮塔发射</param>
        public void StartShoot(Vector3 direction, bool isMine = true)
        {
            _isMyCast = isMine;
            direction.y = 0;  // 只在 XZ 平面移动
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

            // ===== 只有发射者的火球才判定命中 =====
            // 这样避免了"每个客户端都扣一次血"的问题
            if (!_isMyCast) return;
            if (!other.CompareTag("Enemy")) return;

            Debug.Log("触发冲击炮击中效果");

            // 获取命中点
            Vector3 hitPoint = other.ClosestPoint(transform.position);

            // 播放命中特效（本地先行，让发射者立即看到效果）
            PlayHitEffect(hitPoint);
            // 记录攻击次数
            attackCount++;

            // 计算伤害和暴击
            bool isCritical = Random.Range(0, 1f) <= attributePlayerBase.GetCriticalChance();
            float damage = attributePlayerBase.GetAttackPower();

            // ===== 攻击者权威：广播伤害给所有客户端 =====
            if (isTest)
            {
                // 测试模式：直接扣血（不需要网络）
                DealDamageLocal(other.gameObject, damage, isCritical, attributePlayerBase.GetCriticalMultiplier());
            }
            else
            {
                // 联机模式：通过 RPC 广播伤害
                _networkCaster?.BroadcastDamage(
                    other.gameObject,
                    damage,
                    isCritical,
                    attributePlayerBase.GetCriticalMultiplier(),
                    hitPoint
                );
            }
            if (attackCount >= maxAttackCount)
            {
                // 达到最大攻击次数，销毁自身
                RecycleWithEffect();
            }
        }

        /// <summary>
        /// 播放命中特效
        /// </summary>
        private void PlayHitEffect(Vector3 hitPosition)
        {
            string poolKey = isTest ? "ImpactCannonHitTestPool" : "ImpactCannonHitCommonPool";
            GameObject hitEffect = ObjectPoolManager.instance.GetFromPoolAndActivate(poolKey, hitPosition);
            if (hitEffect != null)
            {
                hitEffect.GetComponentInChildren<ParticleSystem>()?.Play();
            }
        }

        /// <summary>
        /// 本地伤害处理（测试模式用）
        /// </summary>
        private void DealDamageLocal(GameObject enemyObj, float damage, bool isCritical, float criticalMultiplier)
        {
            DamageEventArgs damageEventArgs = new DamageEventArgs(
                DamageType.Fire,
                gameObject,
                enemyObj,
                damage,
                isCritical,
                criticalMultiplier
            );

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

        // 令牌 用于绑定特效
        private AttackToken token;
        public void SetToken(AttackToken newToken)
        {
            token = newToken;
        }

        void RecycleWithEffect()
        {
            StopAllCoroutines();

            if (token != null)
            {
                token.RecycleAll();
                token = null;
            }
            else
            {
                Debug.LogWarning("令牌丢失，无法回收所有特效");
                // 降级处理：如果令牌丢失，至少回收自己
                ObjectPoolManager.instance.ReturnToPool(ObjectPoolConst.ImpactCannonTriggerPool, gameObject);
            }
        }
    }
}
