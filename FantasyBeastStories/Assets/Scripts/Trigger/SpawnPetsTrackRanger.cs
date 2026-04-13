using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Trigger
{
    public class SpawnPetsTrackRanger : MonoBehaviour
    {
        private GameObject hostPlayer;
        private GameObject targetEnemy;
        private List<GameObject> enemies = new List<GameObject>();

        void Update()
        {
            UpdateTargetEnemy();
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Enemy"))
            {
                Debug.Log("敌人进入范围：" + other.gameObject.name);
                enemies.Add(other.gameObject);
            }
        }

        void OnTriggerStay(Collider other)
        {

        }

        void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("Enemy") || other.gameObject == null)
            {
                Debug.Log("敌人退出范围：" + other.gameObject.name);
                enemies.Remove(other.gameObject);
            }
        }

        private void UpdateTargetEnemy()
        {
            float minDistance = float.MaxValue;
            //比较敌人距离，选择最近的敌人作为目标
            foreach (GameObject enemy in enemies)
            {
                if (enemy == null)
                {
                    enemies.Remove(enemy);
                    continue;
                }
                float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
                if (distanceToEnemy < minDistance)
                {
                    minDistance = distanceToEnemy;
                    targetEnemy = enemy;
                }
            }
        }

        public GameObject DepatchTargetEnemy()
        {
            return targetEnemy;
        }

        public void SetHostPlayer(GameObject player)
        {
            hostPlayer = player;
        }
    }
}
