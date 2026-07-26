using System.Collections;
using System.Collections.Generic;
using Managers; // TODO: GamePauseManager.isPaused 移到事件通道后移除
using Core;
using Core.Channels.Game;
using Controllers.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Controllers.Enemy
{
    public class AttackableEnemy : EnemyBase
    {
        [Header("NavMesh 设置")]
        [SerializeField]
        protected float pathUpdateInterval = 0.3f; // NavMesh寻路更新间隔

        [Header("LOD 分级优化")]
        [SerializeField, Tooltip("LOD 0 近距离阈值")]
        private float lod0Distance = 10f;

        [SerializeField, Tooltip("LOD 1 中距离阈值")]
        private float lod1Distance = 30f;

        [SerializeField, Tooltip("LOD每隔多久重新计算一次 LOD")]
        private float lodRefreshInterval = 0.5f;

        [SerializeField, Tooltip("LOD 1 攻击检测间隔倍率")]
        private float lod1AttackIntervalMultiplier = 3f;

        [SerializeField, Tooltip("LOD 2 是否冻结动画")]
        private bool lod2FreezeAnimation = true;

        [SerializeField, Tooltip("LOD 2 是否完全禁用攻击检测")]
        private bool lod2DisableAttack = true;

        private NavMeshAgent navMeshAgent;
        private float pathUpdateTimer;

        // 从 EnemyConfigSO 读取的攻击参数（运行时缓存）
        protected float attackDamage;
        protected float attackRange;
        protected float attackInterval;
        protected float attackCooldownTimer = 0f;

        // LOD 运行时状态
        private LODLevel currentLOD = LODLevel.Full;
        private LODLevel previousLOD = LODLevel.Full;
        private float lodRefreshTimer;
        private float sqrLod0;
        private float sqrLod1;
        private float sqrAttackRange;

        /// <summary>
        /// LOD 等级枚举
        /// </summary>
        protected enum LODLevel
        {
            Full,    // 近距离
            Reduced, // 中距离：降级行为，降低攻击检测频率
            Minimal  // 远距离：最小行为，要求冻结动画、禁用 NavMesh、禁用攻击检测
        }

        protected override void Start()
        {
            base.Start();
            navMeshAgent = GetComponent<NavMeshAgent>();

            // 从 SO 配置读取攻击参数
            if (enemyConfig != null)
            {
                attackInterval = enemyConfig.attackInterval;
                attackRange = enemyConfig.attackRange;
                attackDamage = enemyConfig.attackDamage;
            }

            if (navMeshAgent != null)
            {
                navMeshAgent.speed = enemyData.attribute.moveSpeed;
            }

            // 预计算平方距离阈值，避免每帧重复计算
            sqrLod0 = lod0Distance * lod0Distance;
            sqrLod1 = lod1Distance * lod1Distance;
            sqrAttackRange = attackRange * attackRange;
        }

        protected override void Update()
        {
            base.Update();
            if (GamePauseManager.isPaused || enemyData.currentState == EnemyState.Die)
            {
                if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
                    navMeshAgent.isStopped = true;
                return;
            }
            else
            {
                if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
                    navMeshAgent.isStopped = false;
            }

            // 定期刷新 LOD 等级（避免每帧遍历所有玩家）
            lodRefreshTimer -= UnityEngine.Time.deltaTime;
            if (lodRefreshTimer <= 0f)
            {
                UpdateLODLevel();
                lodRefreshTimer = lodRefreshInterval;
            }

            // LOD 2（远距离）且启用了远距离禁用攻击时，跳过攻击检测
            if (currentLOD == LODLevel.Minimal && lod2DisableAttack)
                return;

            DealDamageToPlayers();
        }

        /// <summary>
        /// 根据与最近玩家的距离更新 LOD 等级。
        /// 当 LOD 发生切换时自动触发动画/寻路等组件调整。
        /// </summary>
        private void UpdateLODLevel()
        {
            float sqrDist = GetSqrDistanceToNearestPlayer();

            LODLevel newLevel;
            if (sqrDist <= sqrLod0)
            {
                newLevel = LODLevel.Full;
            }
            else if (sqrDist <= sqrLod1)
            {
                newLevel = LODLevel.Reduced;
            }
            else
            {
                newLevel = LODLevel.Minimal;
            }

            if (newLevel != currentLOD)
            {
                previousLOD = currentLOD;
                currentLOD = newLevel;
                OnLODLevelChanged(previousLOD, currentLOD);
            }
        }

        /// <summary>
        /// LOD 等级切换时的回调
        /// </summary>
        private void OnLODLevelChanged(LODLevel from, LODLevel to)
        {
            bool wasMinimal = from == LODLevel.Minimal;
            bool isMinimal = to == LODLevel.Minimal;

            if (isMinimal && !wasMinimal)
            {
                // 进入 LOD 2（远距离）：冻结动画 + 禁用 NavMesh
                if (lod2FreezeAnimation && animator != null)
                    animator.speed = 0f;

                if (navMeshAgent != null && navMeshAgent.enabled)
                    navMeshAgent.enabled = false;
            }
            else if (!isMinimal && wasMinimal)
            {
                // 离开 LOD 2：恢复动画 + 重新启用 NavMesh
                if (animator != null)
                    animator.speed = 1f;

                if (navMeshAgent != null && !navMeshAgent.enabled)
                {
                    navMeshAgent.enabled = true;
                    navMeshAgent.updatePosition = true;

                    // 查找最近的有效 NavMesh 位置（线性移动可能走离 NavMesh）
                    if (NavMesh.SamplePosition(transform.position, out var hit, 10f, NavMesh.AllAreas))
                    {
                        transform.position = hit.position;
                        navMeshAgent.Warp(hit.position);
                    }

                    navMeshAgent.speed = enemyData.attribute.moveSpeed;
                }
            }
        }

        /// <summary>
        /// 获取距离最近玩家的平方距离
        /// </summary>
        private float GetSqrDistanceToNearestPlayer()
        {
            var players = PlayerManager.instance != null
                ? PlayerManager.instance.ActivePlayerObjects
                : null;

            if (players == null || players.Count == 0)
                return float.MaxValue;

            Vector3 enemyPos = transform.position;
            float minSqrDist = float.MaxValue;

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player == null) continue;

                Vector3 diff = player.transform.position - enemyPos;
                float sqrDist = diff.sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                }
            }

            return minSqrDist;
        }

        /// <summary>
        /// 距离判定：对攻击范围内的所有玩家造成伤害。
        /// LOD 优化：
        /// - LOD 0：正常攻击间隔
        /// - LOD 1：攻击间隔乘以倍率，降低检测频率
        /// - LOD 2：完全禁用攻击检测、冻结动画、禁用collider和ai
        /// </summary>
        private void DealDamageToPlayers()
        {
            if (enemyData.currentState == EnemyState.Die)
                return;

            // LOD 1（中距离）：放大攻击间隔，减少不必要的伤害检测
            float effectiveInterval = attackInterval;
            if (currentLOD == LODLevel.Reduced)
            {
                effectiveInterval = attackInterval * lod1AttackIntervalMultiplier;
            }

            attackCooldownTimer += UnityEngine.Time.deltaTime;
            if (attackCooldownTimer < effectiveInterval)
                return;

            attackCooldownTimer = 0f;

            // 从 PlayerManager 获取所有活跃玩家
            var players = PlayerManager.instance != null
                ? PlayerManager.instance.ActivePlayerObjects
                : null;

            if (players == null || players.Count == 0)
                return;

            Vector3 enemyPos = transform.position;

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player == null) continue;

                // 距离平方比较，避免开根号
                Vector3 diff = player.transform.position - enemyPos;
                if (diff.sqrMagnitude <= sqrAttackRange)
                {
                    DamageEventArgs damageEventArgs = DamageEventArgs.GetShared(
                        Element.Common,
                        gameObject,
                        player,
                        attackDamage,
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

            // 基础血量从 SO 配置读取
            float baseMaxHealth = enemyConfig.maxHealth;

            // 根据游戏时间计算血量倍率（1x → 4x，Boss 出现后回落至 1x）
            float hpMultiplier = EnemyScalingCalculator.GetHpMultiplier(currentTime);

            // 根据玩家数量叠加血量倍率
            int playerCount = PlayerManager.instance != null ? PlayerManager.instance.PlayerCount : 1;
            hpMultiplier *= EnemyScalingCalculator.GetPlayerHpMultiplier(playerCount);

            float newMaxHealth = baseMaxHealth * hpMultiplier;
            enemyData.attribute.maxHealth = newMaxHealth;
            enemyData.attribute.currentHealth = newMaxHealth;

            // 重置 LOD 计时器，从池中取出时立即评估 LOD
            lodRefreshTimer = 0f;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            // 恢复动画速度，防止对象池中间残留冻结状态
            if (animator != null)
                animator.speed = 1f;
            // 恢复 NavMeshAgent，防止池中残留禁用状态
            if (navMeshAgent != null && !navMeshAgent.enabled)
                navMeshAgent.enabled = true;
            TransitionToState(EnemyState.Idle);
        }

        protected override void UpdateRun()
        {
            // 重新评估目标，确保已被移除的死亡玩家不再被追踪
            TrackPlayer();

            if (PlayerTarget == null)
            {
                // 所有玩家死亡：停止移动，保持在 Run 状态（不切换 Idle）
                if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.isStopped = true;
                    navMeshAgent.velocity = Vector3.zero;
                }
                return;
            }

            if (GamePauseManager.isPaused)
                return;

            // LOD 2（远距离）：线性移动，完全绕过 NavMesh 降低成本
            if (currentLOD == LODLevel.Minimal)
            {
                Vector3 direction = (PlayerTarget.transform.position - transform.position).normalized;
                transform.position += direction * enemyData.attribute.moveSpeed * UnityEngine.Time.deltaTime;
                // 仅旋转 Y 轴朝向玩家
                transform.LookAt(new Vector3(
                    PlayerTarget.transform.position.x,
                    transform.position.y,
                    PlayerTarget.transform.position.z));
                return;
            }

            // LOD 0 / 1：正常 NavMesh 寻路
            if (navMeshAgent == null || !navMeshAgent.isActiveAndEnabled || !navMeshAgent.isOnNavMesh)
                return;

            navMeshAgent.isStopped = false;

            // 间隔更新寻路目标
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
            // 恢复 LOD 2 可能影响到的组件状态
            if (animator != null)
                animator.speed = 1f;
            if (navMeshAgent != null && !navMeshAgent.enabled)
                navMeshAgent.enabled = true;

            // 重置 LOD 运行时状态
            currentLOD = LODLevel.Full;
            previousLOD = LODLevel.Full;
            lodRefreshTimer = 0f;
            pathUpdateTimer = 0f;
            attackCooldownTimer = 0f;
        }
    }
}