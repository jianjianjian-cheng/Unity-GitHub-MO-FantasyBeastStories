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

        [SerializeField]
        private bool isTest;
        private int maxAttackCount = 1;
        private int attackCount = 0;
        private AttributePlayerBase attributePlayer;
        private float Speed = 15f;
        private Rigidbody rb;
        private float damageFalloff = 1f; // 伤害衰减系数

        private Vector3 baseScale;

        public bool isSplit = true;

        [SerializeField]
        private float splitRange = 20f; // 搜索敌人的范围
        private int splitCount = 2;
        public bool canSplit = true;
        private GameObject ignoreEnemy;

        [SerializeField]
        private float splitAngle = 30f; // 分裂角度范围

        [SerializeField]
        private float splitDamageMultiplier = 0.5f; // 分裂弹伤害倍率

        void Awake()
        {
            isTest = GameManager.instance != null && GameManager.isTest;
            rb = GetComponent<Rigidbody>();
            _networkCaster = FindObjectOfType<CastNetwork>();
            baseScale = transform.localScale;
        }

        public void OnEnable()
        {
            maxAttackCount = EventManager.instance.TriggerIntReturnCallbackEvent(
                EventNames.OnGetMaxAttackCount_WizardBoy
            );
            Debug.Log($"最大攻击次数：{maxAttackCount}");
            attackCount = 0;
            ignoreEnemy = null;
            canSplit = true;
            damageFalloff = 1f;
            if (attributePlayer?.GetSplit() != null && attributePlayer?.GetSplitCount() != null)
            {
                isSplit = attributePlayer.GetSplit();
                splitCount = attributePlayer.GetSplitCount();
            }
            Invoke("DelayDestorySelf", 0.5f);
        }

        void OnDisable()
        {
            transform.localScale = baseScale;
            Debug.Log("冲击炮被禁用，返回对象池");
            rb.velocity = Vector3.zero;
            CancelInvoke();
        }

        public void SetAttributeFromPlayer(AttributePlayerBase attributePlayer)
        {
            this.attributePlayer = attributePlayer;
        }

        /// <summary>
        /// 发射物体
        /// </summary>
        /// <param name="direction">发射方向</param>
        /// <param name="isMine">是否由本地炮塔发射</param>
        public void StartShoot(Vector3 direction, bool isMine = true)
        {
            if (!GameManager.isTest)
            {
                _isMyCast = isMine;
            }
            direction.y = 0; // 只在 XZ 平面移动
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

            // 防御性检查
            if (attributePlayer == null)
            {
                Debug.LogWarning("[ImpactCannon] attributePlayer 为空");
                return;
            }

            // 检查是否是需要忽略的敌人
            if (ignoreEnemy != null && other.gameObject == ignoreEnemy)
            {
                return;
            }

            if (!other.CompareTag("Enemy"))
                return;

            Vector3 hitPoint = other.ClosestPoint(transform.position);

            // ===== 只有本地玩家创建的火球（包括分裂弹）才判定伤害 =====
            if (_isMyCast)
            {
                Debug.Log("触发冲击炮击中效果");

                // 只有初始火球才能分裂（分裂弹的 canSplit = false）
                if (canSplit)
                {
                    SplitToNearestEnemies(hitPoint, other.gameObject);
                    canSplit = false;
                }

                PlayHitEffect(hitPoint);
                attackCount++;

                bool isCritical = Random.Range(0, 1f) <= attributePlayer.GetCriticalChance();
                float damage = attributePlayer.GetAttackPower() * damageFalloff; // 分裂弹伤害减半

                if (isTest)
                {
                    DealDamageLocal(
                        other.gameObject,
                        damage,
                        isCritical,
                        attributePlayer.GetCriticalMultiplier()
                    );
                }
                else
                {
                    _networkCaster?.BroadcastDamage(
                        other.gameObject,
                        damage,
                        isCritical,
                        attributePlayer.GetCriticalMultiplier(),
                        hitPoint,
                        attributePlayer.GetCurrentElement()
                    );
                }

                if (attackCount >= maxAttackCount)
                {
                    RecycleWithEffect();
                }
            }
            // ===== 其他玩家的火球：只播放视觉效果 =====
            else
            {
                PlayHitEffect(hitPoint);
                RecycleWithEffect();
            }
        }

        private void SplitToNearestEnemies(Vector3 hitPoint, GameObject hitEnemy)
        {
            // 1. 查找范围内所有敌人
            Collider[] enemiesInRange = Physics.OverlapSphere(
                hitPoint,
                splitRange,
                LayerMask.GetMask("Enemy")
            );

            // 2. 按距离排序（排除已命中的敌人）
            List<Collider> validTargets = new List<Collider>();
            foreach (var col in enemiesInRange)
            {
                if (col.gameObject != hitEnemy)
                    validTargets.Add(col);
            }

            Debug.LogWarning($"找到{validTargets.Count}个敌人");
            // 按距离从近到远排序
            validTargets.Sort(
                (a, b) =>
                    Vector3
                        .Distance(hitPoint, a.transform.position)
                        .CompareTo(Vector3.Distance(hitPoint, b.transform.position))
            );
            int actualSplitCount = Mathf.Min(splitCount, validTargets.Count);
            for (int i = 0; i < actualSplitCount; i++)
            {
                Vector3 targetPos = validTargets[i].transform.position;

                // 计算基础方向，只取xz轴方向
                Vector3 xzTargetPos = new Vector3(targetPos.x, hitPoint.y, targetPos.z);
                Vector3 baseDirection = (xzTargetPos - hitPoint).normalized;

                // 添加扇形偏移（让分裂弹看起来更自然）
                Vector3 splitDirection = GetSplitDirection(baseDirection, i, actualSplitCount);

                CreateSplitBullet(hitPoint, splitDirection, validTargets[i].gameObject, hitEnemy);
            }
        }

        /// <summary>
        /// 获取带扇形偏移的方向
        /// </summary>
        private Vector3 GetSplitDirection(Vector3 baseDirection, int index, int total)
        {
            if (total <= 1)
                return baseDirection;

            // 计算偏移角度（均匀分布在扇形内）
            float halfAngle = splitAngle / 2f;
            float step = total > 1 ? splitAngle / (total - 1) : 0;
            float currentAngle = -halfAngle + step * index;

            // 绕Y轴旋转方向
            return Quaternion.Euler(0, currentAngle, 0) * baseDirection;
        }

        /// <summary>
        /// 创建分裂弹
        /// </summary>
        private void CreateSplitBullet(
            Vector3 spawnPos,
            Vector3 direction,
            GameObject targetEnemy,
            GameObject ignoreEnemyObj = null
        )
        {
            string poolName = "";
            switch (attributePlayer.GetCurrentElement())
            {
                case Element.Common:
                    poolName = ObjectPoolConst.ImpactCannonCommonPool;
                    break;
                case Element.Lightning:
                    poolName = ObjectPoolConst.ImpactCannonLightenPool;
                    break;
                case Element.Winter:
                    poolName = ObjectPoolConst.ImpactCannonWinterPool;
                    break;
                default:
                    poolName = ObjectPoolConst.ImpactCannonCommonPool;
                    break;
            }

            // 1. 获取视觉特效
            GameObject visualObj = ObjectPoolManager.instance.GetFromPoolAndActivate(
                poolName,
                spawnPos
            );

            // 2. 获取碰撞触发器
            GameObject triggerObj = ObjectPoolManager.instance.GetFromPoolAndActivate(
                ObjectPoolConst.ImpactCannonTriggerPool,
                spawnPos
            );

            // 3. 检查必要组件
            if (visualObj == null || triggerObj == null)
            {
                Debug.LogWarning("无法从对象池获取分裂弹组件");
                // 清理已获取的对象
                if (visualObj != null)
                    ObjectPoolManager.instance.ReturnToPool(poolName, visualObj);
                if (triggerObj != null)
                    ObjectPoolManager.instance.ReturnToPool(
                        ObjectPoolConst.ImpactCannonTriggerPool,
                        triggerObj
                    );
                return;
            }

            // 4. 设置视觉特效
            if (visualObj != null)
            {
                var particle = visualObj.GetComponentInChildren<ParticleSystem>();
                particle?.Play();
                visualObj.transform.rotation = Quaternion.LookRotation(direction);
            }

            // 5. 设置碰撞触发器
            ImpactCannon splitCannon = triggerObj.GetComponent<ImpactCannon>();
            if (splitCannon != null)
            {
                // 创建令牌并绑定视觉特效和触发器
                AttackToken splitToken = new AttackToken
                {
                    hitCollider = triggerObj,
                    vfxEffect = visualObj,
                    vfxPoolName = poolName,
                };

                splitCannon.SetToken(splitToken);
                splitCannon.StartShoot(direction, true);
                splitCannon.SetAttributeFromPlayer(attributePlayer);
                splitCannon.isSplit = false; // 防止无限分裂
                splitCannon.ignoreEnemy = ignoreEnemyObj;
                splitCannon.canSplit = false;
                splitCannon.damageFalloff = 0.5f;
            }
            if (!isTest && _networkCaster != null)
            {
                _networkCaster.RequestSplitBullet(
                    spawnPos,
                    direction,
                    (int)attributePlayer.GetCurrentElement()
                );
            }
        }

        /// <summary>
        /// 播放命中特效
        /// </summary>
        private void PlayHitEffect(Vector3 hitPosition)
        {
            string poolKey = "";
            switch (attributePlayer.GetCurrentElement())
            {
                case Element.Common:
                    poolKey = ObjectPoolConst.ImpactCannonHitCommonPool;
                    break;
                case Element.Lightning:
                    poolKey = ObjectPoolConst.ImpactCannonHitLightenPool;
                    break;
                case Element.Winter:
                    poolKey = ObjectPoolConst.ImpactCannonHitWinterPool;
                    break;
                case Element.Grass:
                    poolKey = ObjectPoolConst.ImpactCannonHitGrassPool;
                    break;
                default:
                    poolKey = ObjectPoolConst.ImpactCannonHitCommonPool;
                    break;
            }
            GameObject hitEffect = ObjectPoolManager.instance.GetFromPoolAndActivate(
                poolKey,
                hitPosition
            );
            if (hitEffect != null)
            {
                hitEffect.GetComponentInChildren<ParticleSystem>()?.Play();
            }
        }

        /// <summary>
        /// 本地伤害处理（测试模式用）
        /// </summary>
        private void DealDamageLocal(
            GameObject enemyObj,
            float damage,
            bool isCritical,
            float criticalMultiplier,
            Element element = Element.Common
        )
        {
            DamageEventArgs damageEventArgs = new DamageEventArgs(
                element,
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
            ObjectPoolManager.instance.ReturnToPool(
                ObjectPoolConst.ImpactCannonTriggerPool,
                gameObject
            );
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
                ObjectPoolManager.instance.ReturnToPool(
                    ObjectPoolConst.ImpactCannonTriggerPool,
                    gameObject
                );
            }
        }
    }
}
