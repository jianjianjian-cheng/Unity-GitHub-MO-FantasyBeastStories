using System.Collections;
using System.Collections.Generic;
using Atttibute;
using Enemies;
using Manager;
using UnityEngine;

namespace Trigger
{
    /// <summary>
    /// 攻击范围基类：负责检测范围内的敌人，攻击逻辑由子类实现
    /// </summary>
    public abstract class AttackRangeBase : TriggerBase
    {
        protected AttributePlayerBase attributePlayerBase;
        protected List<GameObject> gameObjects = new List<GameObject>();
        protected GameObject targetEnemy;

        [SerializeField]
        protected float offsetY = 0.5f;

        [SerializeField]
        protected float attackInterval = 2f;

        [SerializeField]
        protected float searchRadius = 5f; // 搜索半径（可用于可视化）

        private float attackTimer;

        private int comboCounter = 1;

        private int empowerChargeCounter = 1;
        protected bool isCharged = false;

        public override void Start()
        {
            base.Start();
            attributePlayerBase =
                EventManager.instance != null
                    ? EventManager.instance.GetLocalPlayerAttribute(EventNames.PlayerAttribute_Main)
                    : null;
        }

        public override void Update()
        {
            if (GamePauseManager.isPaused)
                return;
            if (!photonView.IsMine)
            {
                return;
            }
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

            attackInterval = attributePlayerBase.GetAttackInterval();
            // 攻击间隔控制
            if (attackTimer > 0)
            {
                attackTimer -= Time.deltaTime;
                return;
            }
            attackTimer = attackInterval;

            // 调用子类实现的攻击方法
            StartCoroutine(AttackCoroutine());
        }

        //协程，用于短时间内连续攻击,限定攻击次数
        private IEnumerator AttackCoroutine()
        {
            while (comboCounter <= attributePlayerBase.GetComboCount())
            {
                isCharged = (empowerChargeCounter >= attributePlayerBase.GetEmpowerCharge());

                PerformAttack();

                comboCounter++;
                empowerChargeCounter = isCharged ? 1 : empowerChargeCounter + 1;

                yield return new WaitForSeconds(0.3f);
            }
            isCharged = false;
            comboCounter = 1;
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
                transform.position.y + offsetY,
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
            if (searchRadius > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, searchRadius);

                if (targetEnemy != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(GetSpawnPosition(), targetEnemy.transform.position);
                }
            }
        }
    }
}
