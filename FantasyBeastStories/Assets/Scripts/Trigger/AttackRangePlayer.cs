using System.Collections.Generic;
using UnityEngine;
using FX;
using Manager;

namespace Trigger
{
    public class AttackRangePlayer : MonoBehaviour
    {
        [Header("攻击设置")]
        [SerializeField] private float attackInterval = 2f;
        [SerializeField] private Transform firePoint;

        private List<GameObject> gameObjects = new List<GameObject>();
        private GameObject targetEnemy;
        private float attackTimer;

        private void Start()
        {
            Debug.Log("AttackRangePlayer Start");
            if (firePoint == null)
                firePoint = transform;
            attackTimer = 0;
        }

        private void Update()
        {
            UpdateTargetEnemy();
            Debug.Log("攻击目标: " + (targetEnemy != null ? targetEnemy.name : "无"));
            // 计时器逻辑
            if (targetEnemy != null)
            {
                attackTimer += Time.deltaTime;
                if (attackTimer >= attackInterval)
                {
                    Attack();
                    attackTimer = 0f;
                }
            }
            else
            {
                // 没有目标时，重置计时器但不归零（避免切换到有目标时立即攻击）
                attackTimer = attackInterval;
            }
        }

        //更新目标敌人为最近的敌人
        private void UpdateTargetEnemy()
        {
            if (gameObjects.Count > 0)
            {
                targetEnemy = gameObjects[0];
                for (int i = 1; i < gameObjects.Count; i++)
                {
                    if (Vector3.Distance(transform.position, gameObjects[i].transform.position) < Vector3.Distance(transform.position, targetEnemy.transform.position))
                    {
                        targetEnemy = gameObjects[i];
                    }
                }
            }
            else
            {
                targetEnemy = null;
            }
        }

        private void Attack()
        {
            Debug.Log("开始攻击");
            if (targetEnemy == null) return;
            GameObject fireBall = ManagerBase.instance.GetComponent<ObjectPoolManager>().GetFromPoolAndActivate("FireBallPool", firePoint.position);
            if (fireBall != null)
            {
                fireBall.transform.rotation = firePoint.rotation;
                FireBallBase fireBallBase = fireBall.GetComponent<FireBallBase>();
                if (fireBallBase != null)
                {
                    fireBallBase.SetTarget(targetEnemy);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Enemy"))
            {
                Debug.Log("Enemy entered attack range: " + other.gameObject.name);
                if (!gameObjects.Contains(other.gameObject))
                {
                    gameObjects.Add(other.gameObject);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Enemy"))
            {
                Debug.Log("Enemy exited attack range: " + other.gameObject.name);
                gameObjects.Remove(other.gameObject);
            }
        }
    }
}