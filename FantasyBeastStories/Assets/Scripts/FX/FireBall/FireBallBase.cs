using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FX
{
    public class FireBallBase : MonoBehaviour
    {
        protected GameObject tagetEnemy;
        [SerializeField] protected float moveSpeed = 4f; // 移动速度
        // Update is called once per frame
        public virtual void Update()
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

        public virtual void SetTarget(GameObject target)
        {
            tagetEnemy = target;
        }

        public virtual void HandleEnemyCollisionEnter(Collider enemy)
        {

        }

        public virtual void HandleEnemyCollisionStay(Collider enemy)
        {

        }

        public virtual void HandleEnemyCollisionExit(Collider enemy)
        {

        }
    }
}