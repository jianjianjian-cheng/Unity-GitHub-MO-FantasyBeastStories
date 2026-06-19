using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Events;
using Manager;
using Trigger;
using Photon.Pun;
using FireBall_Boss;

namespace Enemies.Bosses
{
    /// <summary>
    /// 蜘蛛Boss - 攻击方式：向前咬、吐火球、连续火球、滚动追踪
    /// </summary>
    public class SpiderBoss : EnemyBase 
    {
        #region 枚举
        private enum BossPhase
        {
            Spawn,      // 登场
            Phase1,     // 一阶段
            Phase2,     // 二阶段
            Death       // 死亡
        }

        private enum BossAction
        {
            Idle,           // 待机
            Bite,           // 向前咬
            RayBeam,       // 单发火球
            FireballBurst,  // 连续火球
            Roll,           // 滚动追踪
            Stunned,        // 硬直
        }
        #endregion

        #region 组件引用
        [Header("组件引用")]
        [SerializeField] private Transform rayBeamPoint;
        [SerializeField] private Transform bitePoint;
        [SerializeField] private GameObject rayBeamPrefab;
        [SerializeField] private GameObject fireballPrefab;
        [SerializeField] private GameObject fireballBurstPoint;
        [SerializeField] private GameObject rollCollider;
        [SerializeField] private NavMeshAgent navMeshAgent;
        #endregion

        #region 攻击参数
        [Header("咬击")]
        [SerializeField] private float biteRange = 2f;
        [SerializeField] private float biteDamage = 3f;
        [SerializeField] private float biteWindUp = 0.4f;
        [SerializeField] private float biteCooldown = 2f;

        [Header("射线")]
        [SerializeField] private float rayBeamSpeed = 8f;
        [SerializeField] private float rayBeamDamage = 5f;
        [SerializeField] private float rayBeamWindUp = 0.5f;
        [SerializeField] private float rayBeamCooldown = 12f;

        [Header("连续火球")]
        [SerializeField] private float fireballSpeed = 2f;
        [SerializeField] private int fireballBurstCount = 5;
        [SerializeField] private float fireballBurstInterval = 0.2f;
        [SerializeField] private float fireballBurstSpread = 15f;
        [SerializeField] private float fireballBurstWindUp = 0.7f;
        [SerializeField] private float fireballBurstCooldown = 8f;

        [Header("滚动追踪")]
        [SerializeField] private float rollSpeed = 12f;
        [SerializeField] private float rollDuration = 2f;
        [SerializeField] private float rollDamage = 5f;
        [SerializeField] private float rollWindUp = 0.6f;
        [SerializeField] private float rollCooldown = 10f;
        [SerializeField] private float rollTurnSpeed = 120f;
        #endregion

        #region 阶段参数
        private IReadOnlyList<GameObject> players;
        private float playerTargetUpdateInterval = 20f;
        [Header("阶段血量阈值")]
        [SerializeField] private float phase2HealthPercent = 0.5f;

        [Header("一阶段")]
        [SerializeField] private float phase1MoveSpeed = 2f;
        [SerializeField] private float phase1PreferredDistance = 5f;

        [Header("二阶段")]
        [SerializeField] private float phase2MoveSpeed = 1f;
        [SerializeField] private float phase2PreferredDistance = 4f;
        [SerializeField] private float phase2CooldownMultiplier = 0.7f;
        #endregion

        #region 防卡死参数
        [Header("防卡死参数")]
        [SerializeField] private float maxAdjustTime = 5f;  // 最大调整时间
        [SerializeField] private float idleActionInterval = 1f;  // 空闲时决策间隔
        #endregion
        private float idleDecisionTimer = 0f;
        private float adjustTimeCounter = 0f;
        #region 内部状态
        [SerializeField]
        private BossPhase currentPhase;
        [SerializeField]
        private BossAction currentAction;
        private Vector2 facingDirection;
        private float currentMoveSpeed;
        private float currentPreferredDistance;

        // 冷却计时器
        private float biteTimer;
        private float rayBeamTimer;
        private float fireballBurstTimer;
        private float rollTimer;

        // NavMesh 寻路更新
        private float pathUpdateTimer;
        private float pathUpdateInterval = 0.3f;

        // 移动状态控制
        private bool isAdjustingPosition;
        private Coroutine adjustPositionCoroutine;
        #endregion

        #region 生命周期
        protected override void Start()
        {
            base.Start();

            // 初始化 NavMeshAgent
            if (navMeshAgent == null)
                navMeshAgent = GetComponent<NavMeshAgent>();

            if (navMeshAgent != null)
            {
                navMeshAgent.updateRotation = false;
                navMeshAgent.autoBraking = true;
                navMeshAgent.speed = phase1MoveSpeed;
                navMeshAgent.acceleration = 3f;
            }

            players =
            PlayerManager.instance != null ? PlayerManager.instance.ActivePlayerObjects : null;

            // 初始化冷却时间，加入随机偏移避免所有Boss同时攻击
            biteTimer = Random.Range(0f, biteCooldown * 0.5f);
            rayBeamTimer = Random.Range(5f, rayBeamCooldown * 0.5f);
            fireballBurstTimer = Random.Range(fireballBurstCooldown * 0.5f, fireballBurstCooldown);
            rollTimer = Random.Range(rollCooldown * 0.5f, rollCooldown);

            StartCoroutine(SpawnSequence());
        }

        protected override void Update()
        {
            base.Update();

            if (GamePauseManager.isPaused || currentState == EnemyState.Die)
            {
                if (navMeshAgent != null && navMeshAgent.enabled)
                    navMeshAgent.isStopped = true;
                return;
            }

            if (currentPhase == BossPhase.Spawn || currentPhase == BossPhase.Death)
                return;

            UpdateCooldowns();
            CheckPhaseTransition();
            UpdatePlayerTargetInterval();

            // 在待机状态下持续面向玩家
            if (currentAction == BossAction.Idle && PlayerTarget != null)
            {
                UpdateFacingInUpdate();
                
                idleDecisionTimer -= Time.deltaTime;
                if (idleDecisionTimer <= 0 && !isAdjustingPosition)
                {
                    DecideNextAction();
                    idleDecisionTimer = idleActionInterval;
                }
            }
        }

        /// <summary>
        /// 在Update中持续更新朝向，避免频繁启动协程
        /// </summary>
        private void UpdateFacingInUpdate()
        {
            if (PlayerTarget == null) return;
            
            Vector3 direction = (PlayerTarget.transform.position - transform.position).normalized;
            direction.y = 0;
            
            if (direction.magnitude < 0.01f) return;
            
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float angle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
            
            // 更新转向动画
            if (Mathf.Abs(angle) > 5f)
            {
                if (angle > 0)
                {
                    animator.SetBool("IsTurningRight", true);
                    animator.SetBool("IsTurningLeft", false);
                }
                else
                {
                    animator.SetBool("IsTurningLeft", true);
                    animator.SetBool("IsTurningRight", false);
                }
            }
            else
            {
                animator.SetBool("IsTurningRight", false);
                animator.SetBool("IsTurningLeft", false);
            }
            
            // 持续旋转，提高旋转速度
            float rotateSpeed = 180f;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime
            );
        }

        /// <summary>
        /// 用于攻击前摇的精确转向（协程版本）
        /// </summary>
        private IEnumerator UpdateFacingCoroutine(Transform targetTransform)
        {
            if (targetTransform == null) yield break;
            
            Vector3 direction = (targetTransform.position - transform.position).normalized;
            direction.y = 0;
            
            if (direction.magnitude < 0.01f) yield break;
            
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float angle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
            
            // 设置动画状态
            if (Mathf.Abs(angle) > 5f)
            {
                animator.SetBool("IsTurningRight", angle > 0);
                animator.SetBool("IsTurningLeft", angle < 0);
            }
            
            // 快速旋转，添加超时机制
            float rotateSpeed = 360f;
            float maxTime = 0.3f;  // 最多等待0.3秒
            float elapsed = 0f;
            
            while (Quaternion.Angle(transform.rotation, targetRotation) > 1f && elapsed < maxTime)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotateSpeed * Time.deltaTime
                );
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 精准对齐
            transform.rotation = targetRotation;
            
            // 重置动画
            animator.SetBool("IsTurningLeft", false);
            animator.SetBool("IsTurningRight", false);
        }

        private void UpdateCooldowns()
        {
            float dt = Time.deltaTime;
            biteTimer -= dt;
            rayBeamTimer -= dt;
            fireballBurstTimer -= dt;
            rollTimer -= dt;
        }

        private float playerTargetUpdateTimer = 0f;
        private void UpdatePlayerTargetInterval()
        {
            playerTargetUpdateTimer += Time.deltaTime;
            if (playerTargetUpdateTimer >= playerTargetUpdateInterval || PhotonNetwork.IsMasterClient)
            {
                UpdatePlayerTarget();
            }
        }

        private void CheckPhaseTransition()
        {
            float hpPercent = attribute.currentHealth / attribute.maxHealth;

            if (hpPercent <= 0)
            {
                StartCoroutine(DeathSequence());
                return;
            }

            // if (currentPhase == BossPhase.Phase1 && hpPercent <= phase2HealthPercent)
            // {
            //     StartCoroutine(TransitionToPhase2());
            // }
        }
        #endregion

        #region 决策
        private void DecideNextAction()
        {
            if (PlayerTarget == null) return;

            float distance = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z), 
                new Vector2(PlayerTarget.transform.position.x, PlayerTarget.transform.position.z)
            );

            // 1. 优先检查是否处于合适距离，如果合适就待机而不是强制攻击
            if (Mathf.Abs(distance - currentPreferredDistance) <= 1f)
            {
                // 距离合适，检查是否有可用技能
                if (HasAvailableAttack())
                {
                    ExecuteBestAttack(distance);
                }
                else
                {
                    // 所有技能冷却中，保持待机
                    currentAction = BossAction.Idle;
                    idleDecisionTimer = idleActionInterval;  // 等下个周期再检查
                }
                return;
            }

            // 2. 距离不合适，优先调整位置
            if (isAdjustingPosition)
            {
                adjustTimeCounter += Time.deltaTime;
                if (adjustTimeCounter > maxAdjustTime)
                {
                    // 超时强制退出调整
                    ForceStopAdjustment();
                    return;
                }
                return;  // 正在调整中，不打断
            }

            // 3. 距离不合适且有可用攻击，尝试边攻击边调整
            if (HasAvailableAttack())
            {
                ExecuteBestAttack(distance);
            }
            else
            {
                // 开始位置调整
                StartAdjustPosition(distance);
            }
        }

        // ========== 新增方法 ==========
        private bool HasAvailableAttack()
        {
            return biteTimer <= 0 || rayBeamTimer <= 0 || 
                fireballBurstTimer <= 0 || rollTimer <= 0;
        }

        private void ExecuteBestAttack(float distance)
        {
            // 近距离优先咬击
            if (biteTimer <= 0 && distance <= biteRange * 1.2f)  
            {
                StartCoroutine(BiteAttack());
                return;
            }

            // 中距离优先激光
            if (rayBeamTimer <= 0 && distance > 4f)
            {
                StartCoroutine(RayBeamAttack());
                return;
            }

            // 远距离优先使用滚动
            if (rollTimer <= 0 && distance > 7f)
            {
                StartCoroutine(RollAttack());
                return;
            }

            // 任意距离可火球burst
            if (fireballBurstTimer <= 0)
            {
                StartCoroutine(FireballBurstAttack());
                return;
            }

        }

        private void StartAdjustPosition(float distance)
        {
            if (adjustPositionCoroutine != null)
            {
                StopCoroutine(adjustPositionCoroutine);
            }
            adjustTimeCounter = 0f;
            adjustPositionCoroutine = StartCoroutine(AdjustPositionWithNavMesh(distance));
        }

        private void ForceStopAdjustment()
        {
            if (adjustPositionCoroutine != null)
            {
                StopCoroutine(adjustPositionCoroutine);
                adjustPositionCoroutine = null;
            }
            
            isAdjustingPosition = false;
            adjustTimeCounter = 0f;
            
            // 强制停止NavMeshAgent
            if (navMeshAgent != null && navMeshAgent.enabled)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.velocity = Vector3.zero;
            }
            
            animator.SetBool("SlowRun", false);
            currentAction = BossAction.Idle;
            
            Debug.LogWarning($"[{gameObject.name}] 位置调整超时，强制退出");
        }
        #endregion

        private IEnumerator AdjustPositionWithNavMesh(float distance)
        {
            if (PlayerTarget == null || navMeshAgent == null || !navMeshAgent.enabled)
            {
                ForceStopAdjustment();
                yield break;
            }

            isAdjustingPosition = true;
            adjustTimeCounter = 0f;
            
            navMeshAgent.speed = currentMoveSpeed;
            navMeshAgent.isStopped = false;

            // 计算目标位置
            Vector3 targetPosition;
            if (distance < currentPreferredDistance - 1f)
            {
                // 后退
                Vector3 awayDir = (transform.position - PlayerTarget.transform.position).normalized;
                targetPosition = transform.position + awayDir * currentPreferredDistance;
                animator.SetBool("SlowRun", true);
            }
            else if (distance > currentPreferredDistance + 1f)
            {
                // 靠近
                targetPosition = PlayerTarget.transform.position;
                animator.SetBool("SlowRun", true);
            }
            else
            {
                // 距离合适
                ForceStopAdjustment();
                yield break;
            }

            // 验证目标点
            if (!NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                // 找不到有效NavMesh点，使用当前位置的最近NavMesh点
                if (!NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas))
                {
                    ForceStopAdjustment();
                    yield break;
                }
            }
            
            navMeshAgent.SetDestination(hit.position);

            // 等待到达目标或超时
            float timeout = maxAdjustTime;
            float elapsed = 0f;
            
            while (elapsed < timeout)
            {
                if (PlayerTarget == null || currentPhase == BossPhase.Death)
                {
                    ForceStopAdjustment();
                    yield break;
                }

                // 检查是否到达目标
                if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.5f)
                {
                    break;
                }

                // 检查是否卡住（NavMeshAgent长时间不动）
                if (navMeshAgent.velocity.magnitude < 0.1f && elapsed > 1f)
                {
                    Debug.LogWarning($"[{gameObject.name}] NavMeshAgent疑似卡住");
                    break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 调整完成
            ForceStopAdjustment();
        }
        private void UpdatePlayerTarget()
        {
            if (players == null || players.Count == 0) return;
            
            float closestDistance = float.MaxValue;
            GameObject closestPlayer = null;
            
            foreach (GameObject player in players)
            {
                if (player == null) continue;
                
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player;
                }
            }
            
            if (closestPlayer != null)
            {
                photonView.RPC("RPC_SyncPlayerTarget", RpcTarget.All, closestPlayer.GetPhotonView().ViewID);
            }
        }


        #region  RPC
        [PunRPC]
        private void RPC_SyncPlayerTarget(int targetViewID)
        {
            PlayerTarget = PhotonView.Find(targetViewID).gameObject;
        }
        [PunRPC]
        private void RPC_SyncTriggerAnim(string animName)
        {
            animator.SetTrigger(animName);
        }


        #endregion

        #region 攻击协程
        private IEnumerator BiteAttack()
        {
            currentAction = BossAction.Bite;
            biteTimer = biteCooldown;

            // 精确转向面向玩家
            yield return StartCoroutine(UpdateFacingCoroutine(PlayerTarget.transform));

            // 攻击时停止移动
            if (navMeshAgent != null)
                navMeshAgent.isStopped = true;

            yield return new WaitForSeconds(0.1f);
            photonView.RPC("RPC_SyncTriggerAnim", RpcTarget.All, "Bite");

            // 前摇
            yield return new WaitForSeconds(biteWindUp);

            // 判定
            Collider[] playersInRange = Physics.OverlapSphere(
                bitePoint.transform.position,
                biteRange,
                LayerMask.GetMask("Player")
            );

            foreach (Collider col in playersInRange)
            {
                if (col.CompareTag("Player"))
                {
                    DamageEventArgs damageEventArgs = new DamageEventArgs(
                        Element.Common,
                        gameObject,
                        col.gameObject,
                        biteDamage * attribute.attackPower,
                        false,
                        1f
                    );

                    EventManager.instance.TriggerEventComplex(
                        EventNames.DamageReceiverPlayer,
                        damageEventArgs
                    );
                }
            }

            // 后摇
            yield return new WaitForSeconds(0.3f);

            currentAction = BossAction.Idle;
        }

        private IEnumerator RayBeamAttack()
        {
            currentAction = BossAction.RayBeam;
            rayBeamTimer = rayBeamCooldown;

            // 精确转向面向玩家
            yield return StartCoroutine(UpdateFacingCoroutine(PlayerTarget.transform));

            // 攻击时停止移动
            if (navMeshAgent != null)
                navMeshAgent.isStopped = true;

            yield return new WaitForSeconds(0.1f);
            photonView.RPC("RPC_SyncTriggerAnim", RpcTarget.All, "Fireball");

            // 前摇
            yield return new WaitForSeconds(rayBeamWindUp);

            // 后摇
            yield return new WaitForSeconds(0.2f);

            currentAction = BossAction.Idle;
        }

        private IEnumerator FireballBurstAttack()
        {
            currentAction = BossAction.FireballBurst;
            fireballBurstTimer = fireballBurstCooldown;

            // 精确转向面向玩家
            yield return StartCoroutine(UpdateFacingCoroutine(PlayerTarget.transform));

            // 攻击时停止移动
            if (navMeshAgent != null)
                navMeshAgent.isStopped = true;

            yield return new WaitForSeconds(0.1f);
            animator.SetBool("IsFireballBurst", true);

            // 前摇
            yield return new WaitForSeconds(fireballBurstWindUp);

            // 连续发射
            for (int i = 0; i < fireballBurstCount; i++)
            {
                if (fireballPrefab != null && rayBeamPoint != null && PlayerTarget != null)
                {
                    // GameObject fb = Instantiate(fireballPrefab, rayBeamPoint.position, Quaternion.identity);
                    // Rigidbody2D rb = fb.GetComponent<Rigidbody2D>();
                    // if (rb != null)
                    // {
                    //     Vector2 baseDir = (PlayerTarget.transform.position - rayBeamPoint.position).normalized;
                    //     // 每发火球加随机散布
                    //     float spread = Random.Range(-fireballBurstSpread, fireballBurstSpread);
                    //     Vector2 dir = Quaternion.Euler(0, 0, spread) * baseDir;
                    //     rb.velocity = dir * rayBeamSpeed;
                    // }
                    //已经在动画中处理
                }
                yield return new WaitForSeconds(fireballBurstInterval);
             }


            animator.SetBool("IsFireballBurst", false);
            // 后摇
            yield return new WaitForSeconds(0.4f);

            currentAction = BossAction.Idle;
        }

        private IEnumerator RollAttack()
        {
            currentAction = BossAction.Roll;
            rollTimer = rollCooldown;

            // 精确转向面向玩家
            yield return StartCoroutine(UpdateFacingCoroutine(PlayerTarget.transform));
            yield return new WaitForSeconds(0.1f);

            // 前摇
            yield return new WaitForSeconds(rollWindUp);
            
            if (navMeshAgent != null)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.speed = rollSpeed;
            }

            if (rollCollider != null)
                rollCollider.SetActive(true);

            Vector3 targetPosition = new Vector3(PlayerTarget.transform.position.x, transform.position.y, PlayerTarget.transform.position.z);
            Vector3 direction = (targetPosition - transform.position).normalized;
            
            // 滚动
            transform.rotation = Quaternion.LookRotation(direction);
            //随机追踪方式(滚动或者加速)
            float ratio = UnityEngine.Random.Range(0f, 1f);
            if (ratio > 0.5f)
            {
                // 滚动
                animator.SetBool("IsRoll", true);
            }
            else
            {
                // 加速
                animator.SetBool("FastRun" , true);
            }
            
            float elapsedTime = 0f;
            while (elapsedTime < rollDuration)
            {
                if (PlayerTarget != null)
                {
                    // 滚动过程中持续追踪玩家
                    targetPosition = new Vector3(PlayerTarget.transform.position.x, transform.position.y, PlayerTarget.transform.position.z);
                    direction = (targetPosition - transform.position).normalized;
                    
                    // 平滑转向
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        targetRotation,
                        rollTurnSpeed * Time.deltaTime
                    );
                }
                
                if (navMeshAgent != null)
                    navMeshAgent.Move(transform.forward * rollSpeed * Time.deltaTime);
                    
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // 收招
            if (ratio > 0.5f)
            {
                // 滚动
                animator.SetBool("IsRoll", false);
            }
            else
            {
                // 加速
                animator.SetBool("FastRun" , false);
            }

            if (navMeshAgent != null)
                navMeshAgent.isStopped = true;
                
            // 关闭滚动碰撞体
            if (rollCollider != null)
                rollCollider.SetActive(false);

            yield return new WaitForSeconds(0.5f);

            currentAction = BossAction.Idle;
        }

        #region 动画调用
        public void StartRayBeam()
        {
            GameObject rayBeam = Instantiate(rayBeamPrefab, rayBeamPoint.position, rayBeamPoint.rotation);
            RayBeam rayBeamScript = rayBeam.GetComponentInChildren<RayBeam>();
            float FinalRayBeamDamage = attribute.GetAttackPower() * rayBeamDamage;
            rayBeamScript.SetOwnerAndAttribute(gameObject, FinalRayBeamDamage);
            StartCoroutine(DelayDestoryRayBeam(rayBeam));
        }
        public void StartfireBallBurst()
        {
            GameObject fireballBurst = Instantiate(fireballPrefab, fireballBurstPoint.transform.position, Quaternion.identity);
            FIreBallProjectile fireBallProjectile = fireballBurst.GetComponent<FIreBallProjectile>();
            fireBallProjectile.SetTargetAndDamage(PlayerTarget.transform, attribute.GetAttackPower() * 6);
        }



        IEnumerator DelayDestoryRayBeam(GameObject rayBeam)
        {
            yield return new WaitForSeconds(1.2f);
            Destroy(rayBeam);
        }

        #endregion


        #endregion

        #region 阶段转换
        private IEnumerator SpawnSequence()
        {
            currentPhase = BossPhase.Spawn;

            if (navMeshAgent != null)
                navMeshAgent.isStopped = true;
                
            photonView.RPC("RPC_SyncTriggerAnim", RpcTarget.All, "Spawn");
            attribute.SetAttackPower(30);
            attribute.SetMaxHealth(100000);
            SyncedGameTimeManager.Instance.InitializeBossUI(100000, "蛛王菲力甫斯");
            // 登场演出
            yield return new WaitForSeconds(2f);

            currentPhase = BossPhase.Phase1;
            currentMoveSpeed = phase1MoveSpeed;
            currentPreferredDistance = phase1PreferredDistance;

            if (navMeshAgent != null)
            {
                navMeshAgent.speed = currentMoveSpeed;
                navMeshAgent.isStopped = false;
            }

            UpdatePlayerTarget();
        }

        private IEnumerator TransitionToPhase2()
        {
            currentPhase = BossPhase.Phase2;

            // 停止当前攻击和移动
            StopAllCoroutines();
            
            // 重置移动状态
            isAdjustingPosition = false;
            adjustPositionCoroutine = null;

            if (navMeshAgent != null)
                navMeshAgent.isStopped = true;

            // 转场演出
            photonView.RPC("RPC_SyncTriggerAnim", RpcTarget.All, "PhaseTransition");  // 可以在动画中添加转场效果
            yield return new WaitForSeconds(1.5f);

            currentMoveSpeed = phase2MoveSpeed;
            currentPreferredDistance = phase2PreferredDistance;

            if (navMeshAgent != null)
            {
                navMeshAgent.speed = currentMoveSpeed;
                navMeshAgent.isStopped = false;
            }

            currentAction = BossAction.Idle;
        }
        #endregion

        #region 死亡
        private IEnumerator DeathSequence()
        {
            currentPhase = BossPhase.Death;
            
            // 重置所有动画状态
            animator.SetBool("IsTurningLeft", false);
            animator.SetBool("IsTurningRight", false);
            animator.SetBool("IsRoll", false);
            animator.SetBool("IsFireballBurst", false);
            
            StopAllCoroutines();
            
            // 重置移动状态
            isAdjustingPosition = false;
            adjustPositionCoroutine = null;

            if (navMeshAgent != null)
                navMeshAgent.isStopped = true;

            // 死亡动画
            photonView.RPC("RPC_SyncTriggerAnim", RpcTarget.All, "Die");
            
            // 死亡演出时间（等待动画播放）
            yield return new WaitForSeconds(2f);

            // 可以在这里添加掉落物、特效等
            Destroy(gameObject);
        }

        protected override void EnterDie()
        {
            base.EnterDie();
            StartCoroutine(DeathSequence());
        }
        #endregion

        #region  重写
        protected override void OnDamageReceived(EventArgsBase args)
        {
            base.OnDamageReceived(args);
            SyncedGameTimeManager.Instance.UpdateHPUI(attribute.currentHealth);
        }
        #endregion

        #region 编辑器
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 绘制咬击范围
            if (bitePoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(bitePoint.position, biteRange);
            }

            // 绘制首选距离
            Gizmos.color = Color.yellow;
            float dist = currentPreferredDistance > 0 ? currentPreferredDistance : phase1PreferredDistance;
            Gizmos.DrawWireSphere(transform.position, dist);
            
            // 绘制移动状态
            if (isAdjustingPosition)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, 1.5f);
            }
            
            // 绘制朝向
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 3f);
        }
#endif
        #endregion

        #region 公共方法（如果需要外部调用）
        /// <summary>
        /// 强制Boss进入硬直状态
        /// </summary>
        public void Stun(float duration)
        {
            StartCoroutine(StunCoroutine(duration));
        }

        private IEnumerator StunCoroutine(float duration)
        {
            currentAction = BossAction.Stunned;
            
            if (navMeshAgent != null)
                navMeshAgent.isStopped = true;
                
            // 停止当前所有协程
            StopAllCoroutines();
            
            // 重置移动状态
            isAdjustingPosition = false;
            adjustPositionCoroutine = null;
            
            
            yield return new WaitForSeconds(duration);
            
            currentAction = BossAction.Idle;
            animator.SetBool("SlowRun" , true);
            if (navMeshAgent != null && currentPhase != BossPhase.Death && currentPhase != BossPhase.Spawn)
                navMeshAgent.isStopped = false;
        }

        /// <summary>
        /// 获取当前Boss阶段
        /// </summary>
        private BossPhase GetCurrentPhase()
        {
            return currentPhase;
        }
        #endregion
    }
}