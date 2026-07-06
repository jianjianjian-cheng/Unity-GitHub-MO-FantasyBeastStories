using System.Collections;
using System.Collections.Generic;
using Domain.Character.Attribute;
using Domain.Event;
using Domain.Player;
using Domain.Enemy;
using Domain.Network;
using UnityEngine;
using Application;

namespace Domain.Combat.Trigger
{
    /// <summary>
    /// 攻击范围基类：负责检测范围内的敌人，攻击逻辑由子类实现
    /// </summary>
    public abstract class AttackRangeBase : TriggerBase
    {
        protected INetworkFireballCaster _networkCaster;
        [SerializeField] protected NetworkIdentityBase _network;

        [Header("纯数据")]
        [SerializeField] private AttackRangeData attackRangeData;

        protected AttributePlayerBase attributePlayerBase;
        protected List<GameObject> gameObjects = new List<GameObject>();
        protected GameObject targetEnemy;

        /// <summary>是否正在连射中（连射期间不再触发新攻击）</summary>
        private bool _isAttacking;

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
            if (PlayerManager.instance != null)
                return PlayerManager.instance.GetLocalPlayerAttribute(AttributeKeyConst.Main);
            return null;
        }

        public override void Update()
        {
            if (GamePauseManager.isPaused)
                return;
            if (!_network.IsMine)
            {
                return;
            }
            if (attributePlayerBase == null)
                attributePlayerBase = GetLocalPlayerAttribute();
            if (attributePlayerBase == null)
                return;

            // 每帧清理已死亡的敌人，确保怪物死亡后及时从列表移除
            CleanupDeadEnemies();

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

            // 攻击间隔中 → 等待计时器归零
            if (attackRangeData.attackTimer > 0)
            {
                attackRangeData.attackTimer -= UnityEngine.Time.deltaTime;
                return;
            }

            // 计时器归零 + 不在连射中 → 启动新一轮连射
            if (!_isAttacking)
            {
                StartCoroutine(AttackSequenceCoroutine());
            }
        }

        /// <summary>
        /// 攻击序列协程：先完成连射，结束后才设置攻击间隔计时器
        /// </summary>
        private IEnumerator AttackSequenceCoroutine()
        {
            _isAttacking = true;

            // ── 阶段一：连射 ──
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

            // ── 阶段二：连射全部完成 → 开始计算攻击间隔 ──
            attackRangeData.attackTimer = attackRangeData.attackInterval;
            _isAttacking = false;
        }

        /// <summary>
        /// 由子类实现的具体攻击逻辑
        /// </summary>
        protected abstract void PerformAttack();

        /// <summary>
        /// 清理已死亡的敌人，确保怪物死亡后及时从 gameObjects 移除
        /// </summary>
        private void CleanupDeadEnemies()
        {
            for (int i = gameObjects.Count - 1; i >= 0; i--)
            {
                var enemyGo = gameObjects[i];
                if (enemyGo == null)
                {
                    gameObjects.RemoveAt(i);
                    continue;
                }

                var enemyBase = enemyGo.GetComponent<EnemyBase>();
                if (enemyBase == null || enemyBase.IsDeadOrDying())
                {
                    gameObjects.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 寻找最近的敌人
        /// </summary>
        protected virtual void UpdateEnemyTarget()
        {
            // 先清理已死亡的敌人
            CleanupDeadEnemies();

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
            // 使用 GetComponentInParent 支持子物体 Collider 的情况
            var enemyBase = other.gameObject.GetComponentInParent<EnemyBase>();
            if (enemyBase != null && !enemyBase.IsDeadOrDying())
            {
                GameObject rootGo = enemyBase.gameObject;
                // 去重：避免同一敌人因多个子 Collider 被重复添加
                if (!gameObjects.Contains(rootGo))
                {
                    gameObjects.Add(rootGo);
                }
            }
        }

        public override void OnTriggerStay(Collider other)
        {
            base.OnTriggerStay(other);
            // 使用 GetComponentInParent 兼容子物体 Collider
            var enemyBase = other.gameObject.GetComponentInParent<EnemyBase>();
            if (enemyBase == null || enemyBase.IsDeadOrDying())
            {
                // 移除时也使用根 GameObject
                if (enemyBase != null)
                    gameObjects.Remove(enemyBase.gameObject);
                else
                    gameObjects.Remove(other.gameObject);
            }
            else
            {
                GameObject rootGo = enemyBase.gameObject;
                if (!gameObjects.Contains(rootGo))
                {
                    gameObjects.Add(rootGo);
                }
            }
        }

        public override void OnTriggerExit(Collider other)
        {
            base.OnTriggerExit(other);
            // 使用 GetComponentInParent 支持子物体 Collider 的情况
            var enemyBase = other.gameObject.GetComponentInParent<EnemyBase>();
            if (enemyBase != null)
            {
                gameObjects.Remove(enemyBase.gameObject);
            }
            else
            {
                gameObjects.Remove(other.gameObject);
            }
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