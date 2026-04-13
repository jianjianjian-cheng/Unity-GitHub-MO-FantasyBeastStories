using System.Collections;
using System.Collections.Generic;
using Enemies;
using FX;
using Manager;
using Trigger;
using UnityEngine;

namespace Trigger
{
    public class AttackRangeBase : TriggerBase
    {
        private bool isTest = false; // 是否测试模式
        private List<GameObject> gameObjects = new List<GameObject>();
        private GameObject targetEnemy;
        private float attackTimer;

        [SerializeField] private float offsetY = 1f;

        [SerializeField] private float attackInterval = 2f;
        // Start is called before the first frame update
        public override void Start()
        {
            base.Start();
            isTest = GameManager.instance != null && GameManager.isTest;
        }

        // Update is called once per frame
        public override void Update()
        {
            base.Update();
            Attack();
        }
        private void Attack()
        {
            UpdateEnemyTarget();
            if (targetEnemy == null) return;
            if (attackTimer > 0)
            {
                attackTimer -= Time.deltaTime;
                return;
            }
            attackTimer = attackInterval;
            Vector3 pos = new Vector3(transform.position.x, transform.position.y + offsetY, transform.position.z);
            if (isTest)
            {
                GameObject gameObject1 = ObjectPoolManager.instance.GetFromPoolAndActivate(ObjectPoolConst.TestPool, pos);
                GameObject trigger1 = ObjectPoolManager.instance.GetFromPoolAndActivate(ObjectPoolConst.ImpactCannonTriggerPool, pos);
                if (targetEnemy != null && gameObject1 != null)
                {
                    gameObject1.GetComponentInChildren<ParticleSystem>().Play();
                    //仅仅在x和z轴上面朝向敌人
                    Vector3 targetPos = new Vector3(targetEnemy.transform.position.x, pos.y, targetEnemy.transform.position.z);
                    gameObject1.transform.LookAt(targetPos);
                    trigger1.GetComponent<ImpactCannon>().StartShoot((targetEnemy.transform.position - pos).normalized);
                }

                return;
            }
            //触发攻击
            GameObject gameObject = ObjectPoolManager.instance.GetFromPoolAndActivate("ImpactCannonCommonPool", pos);
            GameObject trigger = ObjectPoolManager.instance.GetFromPoolAndActivate("ImpactCannonTriggerPool", pos);
            if (targetEnemy != null)
            {
                gameObject.GetComponentInChildren<ParticleSystem>().Play();
                //仅仅在x和z轴上面朝向敌人
                Vector3 targetPos = new Vector3(targetEnemy.transform.position.x, pos.y, targetEnemy.transform.position.z);
                gameObject.transform.LookAt(targetPos);
                trigger.GetComponent<ImpactCannon>().StartShoot((targetEnemy.transform.position - pos).normalized);
            }
        }

        //寻找最近的敌人
        private void UpdateEnemyTarget()
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

        public override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);
            gameObjects.Add(other.gameObject);
        }

        public override void OnTriggerStay(Collider other)
        {
            base.OnTriggerStay(other);
            if (other.gameObject.GetComponent<EnemyBase>() == null)
            {
                gameObjects.Remove(other.gameObject);
                return;
            }
            if (other.gameObject.GetComponent<EnemyBase>().GetIsDie())
            {
                gameObjects.Remove(other.gameObject);
            }
        }

        public override void OnTriggerExit(Collider other)
        {
            base.OnTriggerExit(other);
            gameObjects.Remove(other.gameObject);
        }
    }
}
