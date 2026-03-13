using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FX
{
    public class FireBallBase : MonoBehaviour
    {
        private GameObject tagetEnemy;
        [SerializeField] private float moveSpeed = 4f; // 移动速度
        // Update is called once per frame
        void Update()
        {
            //自动追踪敌人
            if (tagetEnemy == null)
            {
                return;
            }
            transform.LookAt(tagetEnemy.transform);
            // 移动 towards the enemy
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }

        public void SetTarget(GameObject target)
        {
            tagetEnemy = target;
        }
    }
}