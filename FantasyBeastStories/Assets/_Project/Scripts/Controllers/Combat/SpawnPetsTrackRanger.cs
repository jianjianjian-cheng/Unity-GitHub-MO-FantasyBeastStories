using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Controllers.Combat
{
    public class SpawnPetsTrackRanger : MonoBehaviour
    {
        private GameObject hostPlayer;
        private GameObject targetEnemy;
        private List<GameObject> enemies = new List<GameObject>();

        // 用于延迟删除的列表
        private List<GameObject> enemiesToRemove = new List<GameObject>();

        void Update()
        {
            UpdateTargetEnemy();
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Enemy"))
            {
                Debug.Log("敌人进入范围：" + other.gameObject.name);

                // 避免重复添加
                if (!enemies.Contains(other.gameObject))
                {
                    enemies.Add(other.gameObject);
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("Enemy"))
            {
                Debug.Log("敌人退出范围：" + other.gameObject.name);
                enemies.Remove(other.gameObject);
            }
        }

        private void UpdateTargetEnemy()
        {
            // 清理无效的敌人引用
            CleanupNullEnemies();

            // 如果没有敌人，清空目标
            if (enemies.Count == 0)
            {
                targetEnemy = null;
                return;
            }

            float minDistance = float.MaxValue;
            GameObject closestEnemy = null;

            // 查找最近的敌人
            foreach (GameObject enemy in enemies)
            {
                if (enemy == null) continue;

                float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
                if (distanceToEnemy < minDistance)
                {
                    minDistance = distanceToEnemy;
                    closestEnemy = enemy;
                }
            }

            targetEnemy = closestEnemy;
        }

        // 安全的清理方法
        private void CleanupNullEnemies()
        {
            // 方法1：倒序遍历删除（推荐）
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i] == null)
                {
                    enemies.RemoveAt(i);
                }
            }

            // 方法2：使用临时列表（如果你更喜欢这种方式）
            /*
            enemiesToRemove.Clear();
            foreach (GameObject enemy in enemies)
            {
                if (enemy == null)
                {
                    enemiesToRemove.Add(enemy);
                }
            }
            
            foreach (GameObject enemy in enemiesToRemove)
            {
                enemies.Remove(enemy);
            }
            */
        }

        public GameObject DepatchTargetEnemy()
        {
            // 如果目标敌人已失效，重新查找
            if (targetEnemy == null)
            {
                UpdateTargetEnemy();
            }
            return targetEnemy;
        }

        public void SetHostPlayer(GameObject player)
        {
            hostPlayer = player;
        }

        // 可选：定期清理的协程
        void Start()
        {
            StartCoroutine(PeriodicCleanup());
        }

        IEnumerator PeriodicCleanup()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                CleanupNullEnemies();
            }
        }

        // 当物体被销毁时清理
        void OnDestroy()
        {
            enemies.Clear();
            enemiesToRemove.Clear();
        }
    }
}
