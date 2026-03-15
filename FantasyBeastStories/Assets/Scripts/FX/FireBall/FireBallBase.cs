using System.Collections;
using System.Collections.Generic;
using Enemies;
using ExitGames.Client.Photon.StructWrapping;
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
            //如果敌人已经死亡，火球以当前速度和方向直线飞行
            if (tagetEnemy.GetComponent<EnemyBase>().GetIsDie())
            {
                transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
                return;
            }
            // 获取敌人位置
            Transform getHitPos = tagetEnemy.transform.Find("GetHitPos");
            transform.LookAt(getHitPos != null ? getHitPos : tagetEnemy.transform);
            // 移动 towards the enemy
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }

        public virtual void SetTarget(GameObject target)
        {
            tagetEnemy = target;
        }

        public virtual void HandleEnemyCollisionEnter(Collider enemy)
        {
            if (enemy.CompareTag("Enemy"))
            {
                enemy.GetComponent<EnemyBase>().TakeDamage(30f); // 这里的10f是示例伤害值，可以根据需要调整
            }
        }

        public virtual void HandleEnemyCollisionStay(Collider enemy)
        {

        }

        public virtual void HandleEnemyCollisionExit(Collider enemy)
        {

        }
    }
}