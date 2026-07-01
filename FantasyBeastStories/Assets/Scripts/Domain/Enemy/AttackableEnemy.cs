using System.Collections;
using System.Collections.Generic;
using Application; // TODO: GamePauseManager.isPaused 移到事件通道后移除
using Domain.Event;
using Domain.Event.Channels.Game;
using Domain.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Domain.Enemy
{
    /// <summary>
    /// 可攻击玩家的敌人类，继承自 EnemyBase。
    /// 通过距离判定替代碰撞体触发，提高大批量怪物时的性能。
    /// 无攻击动画，怪物紧贴玩家时自动按间隔造成伤害。
    /// </summary>
    public class AttackableEnemy : EnemyBase
    {
        [Header("攻击设置")]
        [SerializeField]
        protected float attackDamage = 10f;

        [SerializeField, Tooltip("攻击距离（怪物与玩家的距离小于此值时造成伤害）")]
        protected float attackRange = 2f;

        [SerializeField]
        protected float pathUpdateInterval = 0.3f; // NavMesh寻路更新间隔（秒）

        private NavMeshAgent navMeshAgent;
        private float pathUpdateTimer;

        protected float attackInterval = 0.7f;
        protected float attackCooldownTimer = 0f;

        protected override void Start()
        {
            base.Start();
            navMeshAgent = GetComponent<NavMeshAgent>();
            if (navMeshAgent != null)
            {
                navMeshAgent.speed = enemyData.attribute.moveSpeed;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (GamePauseManager.isPaused || enemyData.currentState == EnemyState.Die)
            {
                if (navMeshAgent != null)
                    navMeshAgent.isStopped = true;
                return;
            }
            else
            {
                if (navMeshAgent != null)
                    navMeshAgent.isStopped = false;
            }
            DealDamageToPlayers();
        }

        /// <summary>
        /// 距离判定：对攻击范围内的所有玩家造成伤害。
        /// 使用 sqrMagnitude 避免开根号，提高性能。
        /// </summary>
        private void DealDamageToPlayers()
        {
            if (enemyData.currentState == EnemyState.Die)
                return;

            attackCooldownTimer += UnityEngine.Time.deltaTime;
            if (attackCooldownTimer < attackInterval)
                return;

            attackCooldownTimer = 0f;

            // 从 PlayerManager 获取所有活跃玩家
            var players = PlayerManager.instance != null
                ? PlayerManager.instance.ActivePlayerObjects
                : null;

            if (players == null || players.Count == 0)
                return;

            float sqrRange = attackRange * attackRange;
            Vector3 enemyPos = transform.position;

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player == null) continue;

                // 距离平方比较，避免开根号
                Vector3 diff = player.transform.position - enemyPos;
                if (diff.sqrMagnitude <= sqrRange)
                {
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

            // 获取当前游戏时间
            var timeQuery = new TimeQueryData();
            EventChannelLocator.MainContainer.timeQueryChannel?.Query(timeQuery);
            float currentTime = timeQuery.currentTime;

            // 获取玩家数量（单人模式=1，四人模式=4）
            int playerCount = PlayerManager.instance != null ? PlayerManager.instance.PlayerCount : 1;

            // 根据玩家数量计算目标最大血量
            // 1人→3000, 4人→5000, 中间人数线性插值
            float targetMaxHealth = 3000f + (playerCount - 1) * (2000f / 3f);

            // 基于时间的进度：8分钟（480s）达到最大值
            float progress = Mathf.Clamp01(currentTime / 480f);

            // 从基础血量（500）线性增长到目标血量
            float newMaxHealth = Mathf.Lerp(500f, targetMaxHealth, progress);
            enemyData.attribute.maxHealth = newMaxHealth;
            enemyData.attribute.currentHealth = newMaxHealth;
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

            if (GamePauseManager.isPaused)
            {
                navMeshAgent.isStopped = true;
                return;
            }

            navMeshAgent.isStopped = false;

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
            pathUpdateTimer = 0f;
        }

        protected override void EnterDie()
        {
            base.EnterDie();
            if (navMeshAgent != null)
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

        public override void ResetState()
        {
            base.ResetState();
        }
    }
}