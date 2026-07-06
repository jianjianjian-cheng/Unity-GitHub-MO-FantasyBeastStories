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
    ///
    /// LOD (Level of Detail) 分级优化：
    /// 根据与最近玩家的距离动态调整行为精度，远处敌人降低更新频率以提升性能。
    /// 在大批量怪物场景中，远处怪物消耗显著降低。
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

        [Header("LOD 分级优化")]
        [SerializeField, Tooltip("LOD 0 近距离阈值（米），小于此值执行完整行为")]
        private float lod0Distance = 10f;

        [SerializeField, Tooltip("LOD 1 中距离阈值（米），在此值与 lod0 之间执行降级行为")]
        private float lod1Distance = 30f;

        [SerializeField, Tooltip("LOD 等级更新间隔（秒），每隔多久重新计算一次 LOD")]
        private float lodRefreshInterval = 0.5f;

        [SerializeField, Tooltip("LOD 1 攻击检测间隔倍率（越大攻击检测越慢）")]
        private float lod1AttackIntervalMultiplier = 3f;

        [SerializeField, Tooltip("LOD 2 是否冻结动画（animator.speed = 0，恢复时自动还原）")]
        private bool lod2FreezeAnimation = true;

        [SerializeField, Tooltip("LOD 2 是否完全禁用攻击检测")]
        private bool lod2DisableAttack = true;

        private NavMeshAgent navMeshAgent;
        private float pathUpdateTimer;

        protected float attackInterval = 0.7f;
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
            Full,    // 近距离（< lod0Distance）：完整行为
            Reduced, // 中距离（lod0Distance ~ lod1Distance）：降级行为
            Minimal  // 远距离（> lod1Distance）：最小行为
        }

        protected override void Start()
        {
            base.Start();
            navMeshAgent = GetComponent<NavMeshAgent>();
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
        /// LOD 等级切换时的回调，处理动画冻结 / NavMesh 禁用等副作用。
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
        /// 获取距离最近玩家的平方距离（使用 sqrMagnitude 避免开根号）
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
        /// 使用 sqrMagnitude 避免开根号，提高性能。
        ///
        /// LOD 优化：
        /// - LOD 0：正常攻击间隔
        /// - LOD 1：攻击间隔乘以倍率，降低检测频率
        /// - LOD 2：在调用前已被跳过（由 lod2DisableAttack 控制）
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
            // 1人→1000, 2人→1300, 3人→1600, 4人→2000
            float targetMaxHealth = playerCount switch
            {
                1 => 1000f,
                2 => 1300f,
                3 => 1600f,
                4 => 2000f,
                _ => 1000f + (playerCount - 1) * 333f
            };

            // 基于时间的进度：8分钟（480s）达到最大值
            float progress = Mathf.Clamp01(currentTime / 480f);

            // 从基础血量（500）线性增长到目标血量
            float newMaxHealth = Mathf.Lerp(500f, targetMaxHealth, progress);
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
            if (PlayerTarget == null)
                return;

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