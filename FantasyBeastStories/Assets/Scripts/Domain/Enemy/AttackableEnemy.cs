using System.Collections;
using System.Collections.Generic;
using Domain.Event;
using Domain.Event.Channels.Game;
using Domain.Time;
using Domain.Manager;
using UnityEngine;
using UnityEngine.AI;

namespace Domain.Enemy
{
    /// <summary>
    /// 可攻击玩家的敌人类，继承自EnemyBase
    /// 在这个类中实现攻击玩家的逻辑
    /// </summary>
    public class AttackableEnemy : EnemyBase
    {
        [Header("攻击设置")]
        [SerializeField]
        protected float attackDamage = 10f;

        [SerializeField]
        protected float attackCooldown = 1.5f;

        [SerializeField]
        protected LayerMask playerLayer;

        [SerializeField]
        protected float pathUpdateInterval = 0.3f; // NavMesh寻路更新间隔（秒）
        private NavMeshAgent navMeshAgent;
        private List<GameObject> targetAttackPlayers;
        private float pathUpdateTimer; // 寻路更新计时器

        private float QueryDifficultyCoefficient()
        {
            var query = new DifficultyCoefficientQueryData();
            EventChannelLocator.MainContainer.difficultyCoefficientQueryChannel.Raise(query);
            return query.result;
        }

        protected override void Start()
        {
            base.Start();
            targetAttackPlayers = new List<GameObject>();
            navMeshAgent = GetComponent<NavMeshAgent>();
            if (navMeshAgent != null)
            {
                navMeshAgent.speed = enemyData.attribute.moveSpeed;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (EventChannelLocator.MainContainer.gameSettings.IsPaused || enemyData.currentState == EnemyState.Die)
            {
                navMeshAgent.isStopped = true;
                return;
            }
            else
            {
                navMeshAgent.isStopped = false;
            }
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
            if (enemyData.currentState == EnemyState.Die)
                return;
            // 累加时间
            attackCooldownTimer += UnityEngine.Time.deltaTime;

            // 检查是否达到攻击间隔
            if (attackCooldownTimer >= attackInterval)
            {
                // 重置计时器
                attackCooldownTimer = 0f;

                // 对所有目标玩家造成伤害
                foreach (var player in targetAttackPlayers)
                {
                    if (player == null)
                        continue; // 安全检查

                    DamageEventArgs damageEventArgs = new DamageEventArgs(
                        Element.Common,
                        gameObject,
                        player,
                        enemyData.attribute.attackPower,
                        false,
                        0f
                    );
                    EventChannelLocator.MainContainer.playerDamageEventChannel.Raise(damageEventArgs);
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (PlayerTarget == null)
                return;
            if (navMeshAgent != null)
            {
                navMeshAgent.speed = enemyData.attribute.moveSpeed;
                navMeshAgent.updatePosition = true;
            }
            //根据游戏难度更新最大生命值
            if (SyncedGameTimeManager.Instance != null && SyncedGameTimeManager.Instance.GetCurrentTime() > 600f && SyncedGameTimeManager.Instance.GetCurrentTime() < 900f)
            {
                enemyData.attribute.maxHealth = enemyData.attribute.maxHealth * (QueryDifficultyCoefficient() + 2.5f);
            }
            else if (SyncedGameTimeManager.Instance != null && SyncedGameTimeManager.Instance.GetCurrentTime() > 900f && SyncedGameTimeManager.Instance.GetCurrentTime() < 1200f)
            {
                enemyData.attribute.maxHealth = enemyData.attribute.maxHealth * (QueryDifficultyCoefficient() + 3.5f);
            }
            else if (SyncedGameTimeManager.Instance != null && SyncedGameTimeManager.Instance.GetCurrentTime() > 1200f)
            {
                enemyData.attribute.maxHealth = enemyData.attribute.maxHealth * (QueryDifficultyCoefficient() + 4.5f);
            }
            else if (SyncedGameTimeManager.Instance != null && SyncedGameTimeManager.Instance.GetCurrentTime() > 300f && SyncedGameTimeManager.Instance.GetCurrentTime() < 600f)
            {
                enemyData.attribute.maxHealth = enemyData.attribute.maxHealth * QueryDifficultyCoefficient();
            }
            else
            {
                enemyData.attribute.maxHealth = enemyData.attribute.maxHealth * 1;
            }
            enemyData.attribute.currentHealth = enemyData.attribute.maxHealth;
            Debug.Log($"最大生命值: {enemyData.attribute.maxHealth}");
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            TransitionToState(EnemyState.Idle);
        }

        protected override void UpdateRun()
        {
            if (PlayerTarget == null)
                return;
            if (navMeshAgent == null)
                return;

            // 暂停时停止移动
            if (EventChannelLocator.MainContainer.gameSettings.IsPaused)
            {
                navMeshAgent.isStopped = true;
                return;
            }

            navMeshAgent.isStopped = false;

            // 限制寻路更新频率，避免每帧重算路径
            pathUpdateTimer -= UnityEngine.Time.deltaTime;
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
            targetAttackPlayers.Clear();
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

        public override void ResetState()
        {
            base.ResetState();
        }
    }
}