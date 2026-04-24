using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// 可攻击玩家的敌人类，继承自EnemyBase
    /// 在这个类中实现攻击玩家的逻辑
    /// </summary>
    public class AttackableEnemy : EnemyBase
    {
        [Header("攻击设置")]
        [SerializeField] protected float attackRange = 2f;
        [SerializeField] protected float attackDamage = 10f;
        [SerializeField] protected float attackCooldown = 1.5f;
        [SerializeField] protected LayerMask playerLayer;

        protected float lastAttackTime;
        protected bool isAttacking;

        override protected void OnEnable()
        {
            RegisterDamageEvent();
        }

        override protected void OnDisable()
        {
            UnregisterDamageEvent();
        }

        protected override void UpdateRun()
        {
            if (!PlayerTarget)
            {
                TransitionToState(EnemyState.Idle);
                return;
            }

            // 检查是否在攻击范围内
            float distanceToPlayer = Vector3.Distance(transform.position, PlayerTarget.transform.position);
            if (distanceToPlayer <= attackRange)
            {
                TransitionToState(EnemyState.Attack);
                return;
            }

            // 移动向玩家
            Vector3 moveDirection = (PlayerTarget.transform.position - transform.position).normalized;
            rb.MovePosition(transform.position + moveDirection * attribute.moveSpeed * Time.deltaTime);
            transform.LookAt(new Vector3(PlayerTarget.transform.position.x, transform.position.y, PlayerTarget.transform.position.z));
        }

        protected override void EnterAttack()
        {
            isAttacking = false;
            lastAttackTime = Time.time;
        }

        protected override void UpdateAttack()
        {
            if (!PlayerTarget)
            {
                TransitionToState(EnemyState.Idle);
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, PlayerTarget.transform.position);

            // 如果玩家离开攻击范围，返回追踪状态
            if (distanceToPlayer > attackRange * 1.1f) // 稍微扩大脱离范围
            {
                TransitionToState(EnemyState.Run);
                return;
            }

            // 朝向玩家
            transform.LookAt(new Vector3(PlayerTarget.transform.position.x, transform.position.y, PlayerTarget.transform.position.z));

            // 攻击冷却检查
            if (Time.time - lastAttackTime >= attackCooldown && !isAttacking)
            {
                PerformAttack();
            }
        }

        protected virtual void PerformAttack()
        {
            isAttacking = true;
            lastAttackTime = Time.time;

            // 触发攻击动画
            animator?.SetTrigger("attack");

            // 可以在这里添加攻击逻辑，如造成伤害等
            AttackPlayer();

            // 攻击结束
            Invoke(nameof(OnAttackFinished), 0.5f); // 根据动画时间调整
        }

        protected virtual void AttackPlayer()
        {
            if (PlayerTarget == null) return;

            // 检查是否在攻击范围内
            float distanceToPlayer = Vector3.Distance(transform.position, PlayerTarget.transform.position);
            if (distanceToPlayer <= attackRange)
            {
                // 这里实现具体的伤害逻辑
                // 可以通过事件系统或直接调用玩家受伤方法
                Debug.Log($"敌人攻击了 {PlayerTarget.name}，造成 {attackDamage} 点伤害");

                // 示例：发送伤害事件
                // DamageEventArgs damageArgs = new DamageEventArgs(gameObject, PlayerTarget, attackDamage, DamageType.Physical);
                // EventManager.instance.TriggerEvent(EventNames.PlayerDamage, damageArgs);
            }
        }

        protected virtual void OnAttackFinished()
        {
            isAttacking = false;
        }

        protected override void ExitAttack()
        {
            isAttacking = false;
            CancelInvoke(nameof(OnAttackFinished));
        }

        // 在编辑器中可视化攻击范围
        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}