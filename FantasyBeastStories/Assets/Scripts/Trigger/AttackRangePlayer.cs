using System.Collections.Generic;
using UnityEngine;
using FX;
using Manager;
using UnityEngine.Video;

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
            if (firePoint == null)
            {
                GameObject fp = new GameObject("FirePoint");
                fp.transform.parent = transform;
                fp.transform.localPosition = Vector3.up / 1.8f; // 向上1单位
                firePoint = fp.transform;
            }
            attackTimer = 0;
        }

        private void Update()
        {
            UpdateTargetEnemy();
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
                gameObjects.Remove(other.gameObject);
            }
        }
    }
}