using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Trigger
{
    public class AttackRangePlayer : MonoBehaviour
    {
        List<GameObject> gameObjects = new List<GameObject>();
        private GameObject tagetEnemy;

        void Update()
        {
            CaculateDistance();
        }
        //找出最近的敌人
        private void CaculateDistance()
        {
            if (gameObjects.Count == 0)
            {
                tagetEnemy = null;
                return;
            }
            float minDistance = Mathf.Infinity;
            foreach (GameObject enemy in gameObjects)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    tagetEnemy = enemy;
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Enemy"))
            {
                gameObjects.Add(other.gameObject);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Enemy"))
            {
                gameObjects.Remove(other.gameObject);
            }
        }
    }
}
