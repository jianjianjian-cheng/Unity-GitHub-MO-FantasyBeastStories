using System.Collections;
using System.Collections.Generic;
using Domain.Character.Attribute;
using Domain.Event;
using Domain.Event.Channels.Player;
using Domain.Player;
using Domain.Enemy;
using Domain.Manager;
using Domain.Network;
using UnityEngine;

namespace Domain.Combat.Trigger
{
    /// <summary>
    /// 攻击范围基类：负责检测范围内的敌人，攻击逻辑由子类实现
    /// </summary>
    public abstract class AttackRangeBase : TriggerBase
    {
        [SerializeField] protected NetworkIdentityBase _network;

        [Header("纯数据")]
        [SerializeField] private AttackRangeData attackRangeData;

        protected AttributePlayerBase attributePlayerBase;
        protected List<GameObject> gameObjects = new List<GameObject>();
        protected GameObject targetEnemy;

        public override void Start()
        {
            attackRangeData = new AttackRangeData();

            if (_network == null)
                _network = GetComponent<NetworkIdentityBase>();
            if (_network == null)
                Debug.LogError("[AttackRangeBase] NetworkIdentityBase 未赋值，请在预制体 Inspector 中绑定或确保组件存在", this);

            base.Start();
            attributePlayerBase = GetLocalPlayerAttribute();
        }

        private AttributePlayerBase GetLocalPlayerAttribute()
        {
            if (EventChannelLocator.MainContainer != null)
            {
                var query = new PlayerAttributeData(PlayerAttributeQueryType.GetLocalPlayerAttribute)
                { attributeName = AttributeKeyConst.Main };
                EventChannelLocator.MainContainer.playerAttributeChannel.Raise(query);
                if (query.attribute != null)
                    return query.attribute;
            }
            if (PlayerManager.instance != null)
                return PlayerManager.instance.GetLocalPlayerAttribute(AttributeKeyConst.Main);
            return null;
        }

        public override void Update()
        {
            if (EventChannelLocator.MainContainer.gameSettings.IsPaused)
                return;
            if (!_network.IsMine)
            {
                return;
            }
            if (attributePlayerBase == null)
                attributePlayerBase = GetLocalPlayerAttribute();
            if (attributePlayerBase == null)
                return;
            base.Update();
            Attack();
        }

        /// <summary>
        /// 攻击逻辑：基类负责更新目标和控制攻击间隔，具体攻击行为由子类实现
        /// </summary>
        private void Attack()
        {
            UpdateEnemyTarget();

            if (targetEnemy == null)
                return;

            attackRangeData.attackInterval = attributePlayerBase.GetAttackInterval();
            // 攻击间隔控制
            if (attackRangeData.attackTimer > 0)
            {
                attackRangeData.attackTimer -= UnityEngine.Time.deltaTime;
                return;
            }
            attackRangeData.attackTimer = attackRangeData.attackInterval;

            // 调用子类实现的攻击方法
            StartCoroutine(AttackCoroutine());
        }

        //协程，用于短时间内连续攻击,限定攻击次数
        private IEnumerator AttackCoroutine()
        {
            while (attackRangeData.comboCounter <= attributePlayerBase.GetComboCount())
            {
                attackRangeData.isCharged = (attackRangeData.empowerChargeCounter >= attributePlayerBase.GetEmpowerCharge());

                PerformAttack();

                attackRangeData.comboCounter++;
                attackRangeData.empowerChargeCounter = attackRangeData.isCharged ? 1 : attackRangeData.empowerChargeCounter + 1;

                yield return new WaitForSeconds(0.3f);
            }
            attackRangeData.isCharged = false;
            attackRangeData.comboCounter = 1;
        }

        /// <summary>
        /// 由子类实现的具体攻击逻辑
        /// </summary>
        protected abstract void PerformAttack();

        /// <summary>
        /// 寻找最近的敌人
        /// </summary>
        protected virtual void UpdateEnemyTarget()
        {
            if (gameObjects.Count == 0)
            {
                targetEnemy = null;
                return;
            }

            // 清理无效目标（已销毁/已死亡/非敌人）
            for (int i = gameObjects.Count - 1; i >= 0; i--)
            {
                var enemyGo = gameObjects[i];
                if (enemyGo == null)
                {
                    gameObjects.RemoveAt(i);
                    continue;
                }

                var enemyBase = enemyGo.GetComponent<EnemyBase>();
                if (enemyBase == null || enemyBase.GetIsDie())
                {
                    gameObjects.RemoveAt(i);
                }
            }

            if (gameObjects.Count == 0)
            {
                targetEnemy = null;
                return;
            }

            float minDistance = float.MaxValue;
            GameObject closestEnemy = null;

            foreach (GameObject enemy in gameObjects)
            {
                if (enemy == null)
                    continue;

                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = enemy;
                }
            }

            targetEnemy = closestEnemy;
        }

        /// <summary>
        /// 获取目标位置（带Y轴偏移）
        /// </summary>
        protected Vector3 GetSpawnPosition()
        {
            return new Vector3(
                transform.position.x,
                transform.position.y + attackRangeData.offsetY,
                transform.position.z
            );
        }

        /// <summary>
        /// 获取目标方向（忽略Y轴）
        /// </summary>
        protected Vector3 GetTargetDirection()
        {
            if (targetEnemy == null)
                return transform.forward;

            Vector3 pos = GetSpawnPosition();
            Vector3 direction = (targetEnemy.transform.position - pos).normalized;
            direction.y = 0;
            return direction;
        }

        public override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);
            if (other.gameObject.GetComponent<EnemyBase>() != null)
            {
                gameObjects.Add(other.gameObject);
            }
        }

        public override void OnTriggerStay(Collider other)
        {
            base.OnTriggerStay(other);
            var enemyBase = other.gameObject.GetComponent<EnemyBase>();
            if (enemyBase == null || enemyBase.GetIsDie())
            {
                gameObjects.Remove(other.gameObject);
            }
        }

        public override void OnTriggerExit(Collider other)
        {
            base.OnTriggerExit(other);
            gameObjects.Remove(other.gameObject);
        }

        /// <summary>
        /// 在编辑器中可视化攻击范围
        /// </summary>
        protected virtual void OnDrawGizmosSelected()
        {
            if (attackRangeData.searchRadius > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, attackRangeData.searchRadius);

                if (targetEnemy != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(GetSpawnPosition(), targetEnemy.transform.position);
                }
            }
        }
    }
}