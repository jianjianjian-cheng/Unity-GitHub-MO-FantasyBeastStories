using System.Collections;
using System.Collections.Generic;
using Events;
using Manager;
using UnityEngine;
using UnityEngine.AI;

namespace Enemies
{
    /// <summary>
    /// 可攻击玩家的敌人类，继承自EnemyBase
    /// 在这个类中实现攻击玩家的逻辑
    /// </summary>
    public class AttackableEnemy : EnemyBase
    {
        [Header("攻击设置")]
        [SerializeField] protected float attackDamage = 10f;
        [SerializeField] protected float attackCooldown = 1.5f;
        [SerializeField] protected LayerMask playerLayer;
        [SerializeField] protected float pathUpdateInterval = 0.3f; // NavMesh寻路更新间隔（秒）
        private NavMeshAgent navMeshAgent;
        private List<GameObject> targetAttackPlayers;
        private float pathUpdateTimer; // 寻路更新计时器



        protected override void Start()
        {
            base.Start();
            targetAttackPlayers = new List<GameObject>();
            navMeshAgent = GetComponent<NavMeshAgent>();
            if (navMeshAgent != null)
            {
                navMeshAgent.speed = attribute.moveSpeed;
            }
        }

        protected override void Update()
        {
            base.Update();
            DealDamageToPlayers();
        }

        public void OnHandleTriggerEnter(GameObject player)
        {
            targetAttackPlayers.Add(player);
        }

        public void OnHandleTriggerExit(GameObject player)
        {
            targetAttackPlayers.Remove(player);
        }


        protected float attackInterval = 0.7f;
        protected float attackCooldownTimer = 0f;

        private void DealDamageToPlayers()
        {
            if (currentState == EnemyState.Die) return;
            // 累加时间
            attackCooldownTimer += Time.deltaTime;

            // 检查是否达到攻击间隔
            if (attackCooldownTimer >= attackInterval)
            {
                // 重置计时器
                attackCooldownTimer = 0f;

                // 对所有目标玩家造成伤害
                foreach (var player in targetAttackPlayers)
                {
                    if (player == null) continue; // 安全检查

                    DamageEventArgs damageEventArgs = new DamageEventArgs(
                        DamageType.normal,
                        gameObject,
                        player,
                        attribute.attackPower,
                        false,
                        0f
                    );
                    EventManager.instance.TriggerEventComplex(EventNames.DamageReceiverPlayer, damageEventArgs);
                }
            }
        }



        override protected void OnEnable()
        {
            RegisterDamageEvent();
            if (PlayerTarget == null) return;
            if (navMeshAgent != null)
            {
                navMeshAgent.speed = attribute.moveSpeed;
                navMeshAgent.updatePosition = true;
            }
        }

        override protected void OnDisable()
        {
            UnregisterDamageEvent();
            TransitionToState(EnemyState.Idle);
        }

        protected override void UpdateRun()
        {
            if (PlayerTarget == null) return;
            if (navMeshAgent == null) return;

            navMeshAgent.isStopped = false;

            // 限制寻路更新频率，避免每帧重算路径
            pathUpdateTimer -= Time.deltaTime;
            if (pathUpdateTimer <= 0f)
            {
                navMeshAgent.SetDestination(PlayerTarget.transform.position);
                pathUpdateTimer = pathUpdateInterval;
            }
        }

        protected override void EnterRun()
        {
            base.EnterRun();
            pathUpdateTimer = 0f; // 进入Run状态时立即更新一次寻路
        }


        protected override void EnterDie()
        {
            base.EnterDie();
            navMeshAgent.isStopped = true;
        }

        protected override void UpdateDie()
        {
            base.UpdateDie();
        }

        protected override void ExitDie()
        {
            base.ExitDie();
        }

        public override void TakeDamage(DamageEventArgs damageEventArgs)
        {
            base.TakeDamage(damageEventArgs);
        }

        protected override void DropExperience()
        {
            base.DropExperience();
        }
    }
}