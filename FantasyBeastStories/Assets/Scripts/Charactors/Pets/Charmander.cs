using System.Collections;
using System.Collections.Generic;
using Manager;
using Trigger;
using UnityEngine;

namespace Charactors.Pets
{
    public class Charmander : PetsBase
    {
        [Header("玩家跟踪参数")]
        //追踪玩家时间间隔
        [SerializeField] private float playerTrackInterval = 1f;
        private float playerTrackTimer = 0f;
        [SerializeField] private float maxPlayerDistance = 7f;
        [SerializeField] private float minPlayerDistance = 3f;
        [SerializeField] private float playerFollowRadiusMin = 2f;
        [SerializeField] private float playerFollowRadiusMax = 4f;
        [SerializeField] private float arriveDistanceThreshold = 0.5f; // 到达目标点的距离阈值
        [SerializeField] private float playerDirectionChangeThreshold = 45f; // 玩家方向改变角度阈值

        [Header("平滑转向参数")]
        [SerializeField] private float rotationSmoothTime = 0.1f; // 转向平滑时间
        private float rotationVelocity; // 用于SmoothDamp的速度变量

        [Header("状态切换控制")]
        [SerializeField] private float stateChangeCooldown = 0.5f; // 状态切换冷却时间
        private float lastStateChangeTime = 0f; // 上次状态切换的时间
        [SerializeField] private float maxDistanceBuffer = 1.5f; // 最大距离的缓冲区，避免边界频繁切换

        private bool isTrackingPlayer = false; // 是否正在跟踪玩家
        private Vector3 playerFollowPosition;
        private bool hasPlayerFollowPosition = false;
        private Vector3 lastPlayerPosition; // 记录玩家上一帧位置
        private Vector3 lastPlayerDirection; // 记录玩家上一帧方向
        private bool wasOutOfMaxRange = false; // 记录上一帧是否超出最大范围

        GameObject FireFire;
        [SerializeField] private GameObject firefirePos; // 火焰生成位置
        [SerializeField] private GameObject attackFX;
        private ParticleSystem attackFXParticleSystem;
        [SerializeField] private SpawnPetsTrackRanger trackRanger;
        [SerializeField] private float buffer = 1f; // 状态切换缓冲区，避免来回切换

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            // 确保粒子系统组件被正确获取
            if (attackFX != null)
            {
                attackFXParticleSystem = attackFX.GetComponent<ParticleSystem>();
            }

            // 初始化玩家位置记录
            if (hostPlayer != null)
            {
                lastPlayerPosition = hostPlayer.transform.position;
                lastPlayerDirection = Vector3.zero;
            }

            lastStateChangeTime = Time.time;
        }

        protected override void Update()
        {
            base.Update();
            targetEnemy = trackRanger.DepatchTargetEnemy();

            playerTrackTimer += Time.deltaTime;
            // 检查是否需要更新玩家跟踪位置
            if (playerTrackTimer >= playerTrackInterval)
            {
                playerTrackTimer = 0f;
                TrackPlayer();
            }

            // 【核心修改】首先检查与玩家的距离，超出最大距离优先追踪玩家
            if (hostPlayer != null)
            {
                float distanceToPlayer = Vector3.Distance(
                    new Vector3(transform.position.x, 0, transform.position.z),
                    new Vector3(hostPlayer.transform.position.x, 0, hostPlayer.transform.position.z)
                );

                // 使用缓冲区的距离判断，避免边界频繁切换
                float effectiveMaxDistance = wasOutOfMaxRange ? maxPlayerDistance - maxDistanceBuffer : maxPlayerDistance;

                // 如果超出最大距离，强制追踪玩家，中断一切其他行为
                if (distanceToPlayer > effectiveMaxDistance)
                {
                    wasOutOfMaxRange = true;

                    if (!isTrackingPlayer && CanChangeState())
                    {
                        // 中断当前行为，开始追踪玩家
                        SetRandomPlayerFollowPosition();
                        isTrackingPlayer = true;
                        ChangeState(PetState.Run);
                        Debug.Log($"超出最大距离({distanceToPlayer} > {effectiveMaxDistance})，强制追踪玩家");
                    }
                    return; // 直接返回，不处理敌人追踪
                }
                else
                {
                    wasOutOfMaxRange = false;
                }
            }

            // 只有在最大距离范围内，才处理敌人追踪
            // 追踪敌人（优先级高于追踪玩家）
            // 如果在追踪玩家过程中发现敌人，中断追踪并攻击敌人
            if (targetEnemy != null)
            {
                float distanceToEnemy = Vector3.Distance(transform.position, targetEnemy.transform.position);
                float distanceToPlayer = hostPlayer != null ?
                    Vector3.Distance(transform.position, hostPlayer.transform.position) : float.MaxValue;

                // 如果敌人在攻击范围内，或者敌人比玩家更近，优先处理敌人
                if (distanceToEnemy <= attackDistance || distanceToEnemy < distanceToPlayer)
                {
                    // 如果在追踪玩家，中断追踪
                    if (isTrackingPlayer && CanChangeState())
                    {
                        isTrackingPlayer = false;
                        hasPlayerFollowPosition = false;
                    }
                    TrackTarget(targetEnemy);
                }
                else if (!isTrackingPlayer)
                {
                    // 没有追踪玩家，正常追踪敌人
                    TrackTarget(targetEnemy);
                }
            }
            else if (!isTrackingPlayer && CanChangeState())
            {
                // 检查是否需要开始追踪玩家（即使没有敌人，如果距离太远也要追踪）
                if (hostPlayer != null)
                {
                    float distanceToPlayer = Vector3.Distance(
                        new Vector3(transform.position.x, 0, transform.position.z),
                        new Vector3(hostPlayer.transform.position.x, 0, hostPlayer.transform.position.z)
                    );

                    if (distanceToPlayer > minPlayerDistance)
                    {
                        SetRandomPlayerFollowPosition();
                        isTrackingPlayer = true;
                        ChangeState(PetState.Run);
                    }
                    else if (currentState != PetState.Idle && currentState != PetState.Attack && CanChangeState())
                    {
                        ChangeState(PetState.Idle);
                    }
                }
                else if (currentState != PetState.Idle && CanChangeState())
                {
                    ChangeState(PetState.Idle);
                }
            }
        }

        // 检查是否可以切换状态（冷却时间检查）
        private bool CanChangeState()
        {
            return Time.time - lastStateChangeTime >= stateChangeCooldown;
        }

        // 切换状态时记录时间
        protected new void ChangeState(PetState newState)
        {
            if (currentState == newState) return;

            base.ChangeState(newState);
            lastStateChangeTime = Time.time;
            Debug.Log($"状态切换: {currentState} -> {newState}");
        }

        #region 状态机
        protected override void IdleEnter()
        {
            base.IdleEnter();
            isTrackingPlayer = false;
            hasPlayerFollowPosition = false;
            rb.velocity = Vector3.zero;
        }

        protected override void IdleStay()
        {
            base.IdleStay();

            // 【核心修改】在Idle状态下首先检查与玩家的距离
            if (hostPlayer != null)
            {
                float distanceToPlayer = Vector3.Distance(
                    new Vector3(transform.position.x, 0, transform.position.z),
                    new Vector3(hostPlayer.transform.position.x, 0, hostPlayer.transform.position.z)
                );

                float effectiveMaxDistance = wasOutOfMaxRange ? maxPlayerDistance - maxDistanceBuffer : maxPlayerDistance;

                // 如果超出最大距离，强制追踪玩家
                if (distanceToPlayer > effectiveMaxDistance && CanChangeState())
                {
                    SetRandomPlayerFollowPosition();
                    isTrackingPlayer = true;
                    ChangeState(PetState.Run);
                    Debug.Log($"IdleStay: 超出最大距离({distanceToPlayer} > {effectiveMaxDistance})，强制追踪玩家");
                    return;
                }
            }

            if (!CanChangeState()) return;

            // 在Idle状态下检查是否需要移动
            // 只有在最大距离范围内才考虑敌人
            if (targetEnemy != null)
            {
                float distance = Vector3.Distance(transform.position, targetEnemy.transform.position);
                if (distance > attackDistance + buffer)
                {
                    ChangeState(PetState.Run);
                }
                else if (distance <= attackDistance - buffer)
                {
                    ChangeState(PetState.Attack);
                }
            }
            // 如果没有敌人但正在追踪玩家
            else if (isTrackingPlayer && hasPlayerFollowPosition)
            {
                ChangeState(PetState.Run);
            }
            // 检查是否离玩家太远
            else if (hostPlayer != null)
            {
                float distanceToPlayer = Vector3.Distance(
                    new Vector3(transform.position.x, 0, transform.position.z),
                    new Vector3(hostPlayer.transform.position.x, 0, hostPlayer.transform.position.z)
                );

                if (distanceToPlayer > minPlayerDistance + buffer)
                {
                    SetRandomPlayerFollowPosition();
                    isTrackingPlayer = true;
                    ChangeState(PetState.Run);
                }
            }
        }

        protected override void IdleExit()
        {
            base.IdleExit();
        }

        protected override void AttackEnter()
        {
            base.AttackEnter();
            rb.velocity = Vector3.zero;
            if (attackFXParticleSystem != null)
            {
                attackFXParticleSystem.Play(true);
            }
        }

        protected override void AttackStay()
        {
            base.AttackStay();

            // 【核心修改】在攻击状态下也要检查与玩家的距离
            if (hostPlayer != null)
            {
                float distanceToPlayer = Vector3.Distance(
                    new Vector3(transform.position.x, 0, transform.position.z),
                    new Vector3(hostPlayer.transform.position.x, 0, hostPlayer.transform.position.z)
                );

                // 使用更大的缓冲区，避免在攻击时频繁切换
                float attackToRunBuffer = maxDistanceBuffer * 1.5f;
                float effectiveMaxDistance = wasOutOfMaxRange ? maxPlayerDistance - attackToRunBuffer : maxPlayerDistance;

                // 如果超出最大距离，立即中断攻击，开始追踪玩家
                if (distanceToPlayer > effectiveMaxDistance && CanChangeState())
                {
                    SetRandomPlayerFollowPosition();
                    isTrackingPlayer = true;
                    ChangeState(PetState.Run);
                    Debug.Log($"AttackStay: 超出最大距离({distanceToPlayer} > {effectiveMaxDistance})，强制中断攻击追踪玩家");
                    return;
                }
            }

            // 攻击状态下持续面向敌人（使用平滑转向）
            if (targetEnemy != null)
            {
                Vector3 targetPosition = new Vector3(
                    targetEnemy.transform.position.x,
                    transform.position.y,
                    targetEnemy.transform.position.z
                );
                SmoothLookAt(targetPosition);

                // 检查敌人是否超出攻击范围
                float distanceToEnemy = Vector3.Distance(transform.position, targetEnemy.transform.position);
                if (distanceToEnemy > attackDistance + buffer && CanChangeState())
                {
                    ChangeState(PetState.Run);
                }
            }
            else
            {
                // 如果敌人消失了，退出攻击状态
                if (CanChangeState())
                {
                    ChangeState(PetState.Idle);
                }
            }
        }

        protected override void AttackExit()
        {
            base.AttackExit();
            if (attackFXParticleSystem != null)
            {
                attackFXParticleSystem.Stop();
            }
        }

        protected override void RunEnter()
        {
            animator.SetBool("isRun", true);
            animator.SetBool("isAttack", false);

            UpdateRunVelocity();
        }

        protected override void RunStay()
        {
            base.RunStay();

            // 【核心修改】在Run状态下首先检查与玩家的距离
            if (hostPlayer != null)
            {
                float distanceToPlayer = Vector3.Distance(
                    new Vector3(transform.position.x, 0, transform.position.z),
                    new Vector3(hostPlayer.transform.position.x, 0, hostPlayer.transform.position.z)
                );

                float effectiveMaxDistance = wasOutOfMaxRange ? maxPlayerDistance - maxDistanceBuffer : maxPlayerDistance;

                // 如果超出最大距离，确保是在追踪玩家状态
                if (distanceToPlayer > effectiveMaxDistance && !isTrackingPlayer && CanChangeState())
                {
                    SetRandomPlayerFollowPosition();
                    isTrackingPlayer = true;
                    UpdateRunVelocity();
                    Debug.Log($"RunStay: 超出最大距离({distanceToPlayer} > {effectiveMaxDistance})，强制追踪玩家");
                    return;
                }
            }

            // 在追踪玩家时，实时检测玩家方向改变
            if (isTrackingPlayer && hasPlayerFollowPosition && hostPlayer != null)
            {
                // 计算玩家当前移动方向
                Vector3 currentPlayerDirection = (hostPlayer.transform.position - lastPlayerPosition).normalized;

                if (lastPlayerDirection != Vector3.zero && currentPlayerDirection != Vector3.zero)
                {
                    float angle = Vector3.Angle(lastPlayerDirection, currentPlayerDirection);
                    if (angle > playerDirectionChangeThreshold)
                    {
                        Debug.Log($"RunStay中检测到玩家方向改变，角度: {angle}");
                        SetRandomPlayerFollowPosition();
                        UpdateRunVelocity();

                        // 更新记录
                        lastPlayerDirection = currentPlayerDirection;
                        lastPlayerPosition = hostPlayer.transform.position;
                        return;
                    }
                }

                // 更新记录的玩家方向和位置
                lastPlayerDirection = currentPlayerDirection;
                lastPlayerPosition = hostPlayer.transform.position;
            }

            // 【核心修改】只有在最大距离范围内才考虑攻击敌人
            bool canAttackEnemy = true;
            if (hostPlayer != null)
            {
                float distanceToPlayer = Vector3.Distance(
                    new Vector3(transform.position.x, 0, transform.position.z),
                    new Vector3(hostPlayer.transform.position.x, 0, hostPlayer.transform.position.z)
                );
                // 使用更严格的判断，确保在安全距离内才能攻击
                canAttackEnemy = distanceToPlayer <= maxPlayerDistance - buffer;
            }

            // 在Run状态下持续检查是否有更高优先级的目标
            if (targetEnemy != null && canAttackEnemy && CanChangeState())
            {
                float distanceToEnemy = Vector3.Distance(transform.position, targetEnemy.transform.position);

                // 如果敌人在攻击范围内，立即切换到攻击
                if (distanceToEnemy <= attackDistance - buffer)
                {
                    // 如果在追踪玩家，中断追踪
                    if (isTrackingPlayer)
                    {
                        isTrackingPlayer = false;
                        hasPlayerFollowPosition = false;
                    }
                    ChangeState(PetState.Attack);
                    return;
                }
                // 如果敌人比当前目标更近且当前在追踪玩家，切换到追踪敌人
                else if (isTrackingPlayer)
                {
                    float distanceToPlayerTarget = hasPlayerFollowPosition ?
                        Vector3.Distance(transform.position, playerFollowPosition) : float.MaxValue;

                    if (distanceToEnemy < distanceToPlayerTarget)
                    {
                        isTrackingPlayer = false;
                        hasPlayerFollowPosition = false;
                        UpdateRunVelocity();
                        return;
                    }
                }
            }

            // 根据当前目标更新移动
            UpdateRunVelocity();

            // 检查是否到达目标
            if (isTrackingPlayer && hasPlayerFollowPosition)
            {
                float distanceToTarget = Vector3.Distance(
                    new Vector3(transform.position.x, 0, transform.position.z),
                    new Vector3(playerFollowPosition.x, 0, playerFollowPosition.z)
                );

                if (distanceToTarget <= arriveDistanceThreshold)
                {
                    // 到达目标点后，检查是否仍在玩家允许区域内
                    if (hostPlayer != null)
                    {
                        float distanceToPlayer = Vector3.Distance(
                            new Vector3(transform.position.x, 0, transform.position.z),
                            new Vector3(hostPlayer.transform.position.x, 0, hostPlayer.transform.position.z)
                        );

                        if (distanceToPlayer > minPlayerDistance + buffer)
                        {
                            // 仍然在允许区域外，继续追踪
                            Debug.Log("到达追踪点但仍离玩家太远，继续追踪");
                            SetRandomPlayerFollowPosition();
                            UpdateRunVelocity();
                            return;
                        }
                    }

                    // 已在允许区域内，停止追踪
                    if (CanChangeState())
                    {
                        isTrackingPlayer = false;
                        hasPlayerFollowPosition = false;
                        rb.velocity = Vector3.zero;
                        ChangeState(PetState.Idle);
                    }
                }
            }
            else if (targetEnemy != null && !isTrackingPlayer && canAttackEnemy && CanChangeState())
            {
                float distance = Vector3.Distance(transform.position, targetEnemy.transform.position);
                if (distance <= attackDistance - buffer)
                {
                    ChangeState(PetState.Attack);
                }
            }
            else if (!isTrackingPlayer && targetEnemy == null && CanChangeState())
            {
                // 检查是否离玩家太远
                if (hostPlayer != null)
                {
                    float distanceToPlayer = Vector3.Distance(
                        new Vector3(transform.position.x, 0, transform.position.z),
                        new Vector3(hostPlayer.transform.position.x, 0, hostPlayer.transform.position.z)
                    );

                    if (distanceToPlayer > minPlayerDistance + buffer)
                    {
                        SetRandomPlayerFollowPosition();
                        isTrackingPlayer = true;
                        UpdateRunVelocity();
                        return;
                    }
                }
                ChangeState(PetState.Idle);
            }
        }

        protected override void RunExit()
        {
            base.RunExit();
            rb.velocity = Vector3.zero;
        }
        #endregion

        private void UpdateRunVelocity()
        {
            Vector3 direction = Vector3.zero;
            Vector3 targetPosition = Vector3.zero;

            // 确定移动目标
            if (targetEnemy != null && !isTrackingPlayer)
            {
                // 追踪敌人
                targetPosition = targetEnemy.transform.position;
            }
            else if (isTrackingPlayer && hasPlayerFollowPosition)
            {
                // 追踪玩家
                targetPosition = playerFollowPosition;
            }
            else if (hostPlayer != null)
            {
                // 默认朝向玩家
                targetPosition = hostPlayer.transform.position;
            }
            else
            {
                rb.velocity = Vector3.zero;
                return;
            }

            // 计算移动方向
            direction = (targetPosition - transform.position).normalized;
            direction.y = 0;

            // 使用平滑转向
            Vector3 lookAtPosition = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
            SmoothLookAt(lookAtPosition);

            // 设置速度
            if (direction != Vector3.zero)
            {
                rb.velocity = direction * moveSpeed;
            }
            else
            {
                rb.velocity = Vector3.zero;
            }
        }

        // 平滑转向方法
        private void SmoothLookAt(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float currentAngle = transform.eulerAngles.y;
                float smoothedAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref rotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);
            }
        }

        private void TrackTarget(GameObject targetEnemy)
        {
            // 如果正在追踪玩家且敌人不在优先范围内，不处理敌人追踪
            if (targetEnemy == null) return;

            float distance = Vector3.Distance(transform.position, targetEnemy.transform.position);

            // 根据距离决定状态切换
            if (distance > attackDistance + buffer)
            {
                if (currentState != PetState.Run && CanChangeState())
                {
                    ChangeState(PetState.Run);
                }
            }
            else if (distance <= attackDistance - buffer)
            {
                if (currentState != PetState.Attack && CanChangeState())
                {
                    ChangeState(PetState.Attack);
                }
            }
            // 在缓冲区范围内保持当前状态
        }

        private void TrackPlayer()
        {
            if (hostPlayer == null) return;

            float distance = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(hostPlayer.transform.position.x, 0, hostPlayer.transform.position.z)
            );

            float effectiveMaxDistance = wasOutOfMaxRange ? maxPlayerDistance - maxDistanceBuffer : maxPlayerDistance;

            // 【核心修改】如果超出最大距离，强制追踪玩家（无论是否在攻击）
            if (distance > effectiveMaxDistance)
            {
                if (!isTrackingPlayer && CanChangeState())
                {
                    SetRandomPlayerFollowPosition();
                    isTrackingPlayer = true;
                    ChangeState(PetState.Run);
                    Debug.Log($"TrackPlayer: 超出最大距离({distance} > {effectiveMaxDistance})，强制追踪玩家");
                }
                return;
            }

            // 计算玩家当前移动方向
            Vector3 currentPlayerDirection = (hostPlayer.transform.position - lastPlayerPosition).normalized;
            bool playerDirectionChanged = false;

            // 检查玩家方向是否发生显著改变
            if (lastPlayerDirection != Vector3.zero && currentPlayerDirection != Vector3.zero)
            {
                float angle = Vector3.Angle(lastPlayerDirection, currentPlayerDirection);
                playerDirectionChanged = angle > playerDirectionChangeThreshold;
            }

            // 更新记录的玩家位置和方向
            lastPlayerDirection = currentPlayerDirection;
            lastPlayerPosition = hostPlayer.transform.position;

            // 如果已经在追踪玩家
            if (isTrackingPlayer)
            {
                if (hasPlayerFollowPosition)
                {
                    float distanceToTarget = Vector3.Distance(
                        new Vector3(transform.position.x, 0, transform.position.z),
                        new Vector3(playerFollowPosition.x, 0, playerFollowPosition.z)
                    );

                    // 如果玩家方向改变，重新设置追踪点
                    if (playerDirectionChanged)
                    {
                        Debug.Log("玩家方向改变，重新设置追踪点");
                        SetRandomPlayerFollowPosition();
                        if (CanChangeState())
                        {
                            ChangeState(PetState.Run);
                        }
                    }
                    else if (distanceToTarget <= arriveDistanceThreshold)
                    {
                        // 到达目标点后，检查是否仍在玩家允许区域内
                        if (distance > minPlayerDistance + buffer)
                        {
                            // 仍然在允许区域外，继续追踪
                            Debug.Log("到达追踪点但仍离玩家太远，继续追踪");
                            SetRandomPlayerFollowPosition();
                            if (CanChangeState())
                            {
                                ChangeState(PetState.Run);
                            }
                        }
                        else
                        {
                            // 已在允许区域内，停止追踪
                            isTrackingPlayer = false;
                            hasPlayerFollowPosition = false;
                            if (currentState != PetState.Attack && CanChangeState())
                            {
                                ChangeState(PetState.Idle);
                            }
                        }
                    }
                }
                else
                {
                    // 有追踪标志但没有目标位置，重置状态
                    isTrackingPlayer = false;
                    if (currentState != PetState.Attack && CanChangeState())
                    {
                        ChangeState(PetState.Idle);
                    }
                }
                return;
            }

            // 检查是否需要开始追踪玩家（只在最大距离范围内才考虑敌人优先级）
            bool shouldTrackPlayer = false;

            if (targetEnemy == null)
            {
                // 没有敌人时，根据距离决定是否追踪玩家
                shouldTrackPlayer = distance > minPlayerDistance + buffer;
            }
            else
            {
                // 有敌人但敌人距离较远，且玩家距离也较远
                float distanceToEnemy = Vector3.Distance(transform.position, targetEnemy.transform.position);
                shouldTrackPlayer = distance > minPlayerDistance + buffer && distanceToEnemy > attackDistance + buffer;
            }

            if (shouldTrackPlayer && CanChangeState())
            {
                SetRandomPlayerFollowPosition();
                isTrackingPlayer = true;
                ChangeState(PetState.Run);
            }
        }

        private void SetRandomPlayerFollowPosition()
        {
            if (hostPlayer == null) return;

            float angle = Random.Range(0f, 360f);
            float radius = Random.Range(playerFollowRadiusMin, playerFollowRadiusMax);
            Vector3 offset = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                0f,
                Mathf.Sin(angle * Mathf.Deg2Rad)
            ) * radius;

            playerFollowPosition = hostPlayer.transform.position + offset;
            hasPlayerFollowPosition = true;

            Debug.Log($"设置新的玩家跟随位置: {playerFollowPosition}, 距离玩家: {radius}");
        }

        // 可选：添加Gizmos可视化调试
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (hostPlayer != null)
            {
                // 绘制追踪范围
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(hostPlayer.transform.position, minPlayerDistance);

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(hostPlayer.transform.position, maxPlayerDistance);

                // 绘制缓冲区范围
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                Gizmos.DrawWireSphere(hostPlayer.transform.position, maxPlayerDistance - maxDistanceBuffer);

                // 绘制随机跟随范围
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(hostPlayer.transform.position, playerFollowRadiusMin);
                Gizmos.DrawWireSphere(hostPlayer.transform.position, playerFollowRadiusMax);
            }

            if (isTrackingPlayer && hasPlayerFollowPosition)
            {
                // 绘制目标点
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(playerFollowPosition, 0.5f);
                Gizmos.DrawLine(transform.position, playerFollowPosition);
            }

            if (targetEnemy != null)
            {
                // 绘制攻击距离
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position, attackDistance);
            }
        }
        public void SetHostPlayer(GameObject hostPlayer)
        {
            this.hostPlayer = hostPlayer; // 设置宠物的主人玩家
        }
#endif
    }
}