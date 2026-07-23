using System.Collections;
using System.Collections.Generic;
using Controllers.Character;
using Core;
using Core.Channels.Combat;
using Controllers.Player;
using Core;
using Controllers.PowerUp;
using Core.Contracts;
using Core.Network;
using NetworkTarget = Controllers.Network.NetworkTarget;
using Controllers.Network;
using UnityEngine;
using UnityEngine.AI;
using Managers;

namespace Controllers.Enemy
{
  public class EnemyBase : MonoBehaviour
  {
    [Header("敌人配置")]
    [SerializeField]
    protected EnemyConfigSO enemyConfig;

    [Header("纯数据")]
    [SerializeField]
    protected EnemyData enemyData;

    [Header("事件通道")]
    [SerializeField] private DamageEventChannelSO damageEventChannel;

    [SerializeField] protected NetworkIdentityBase _network;


    protected Collider[] colliders;

    [SerializeField]
    protected Animator animator;

    [SerializeField]
    protected Rigidbody rb;

    [SerializeField]
    protected GameObject PlayerTarget;


    protected virtual void Awake()
    {
      if (enemyConfig == null)
      {
        Debug.LogError($"[EnemyBase] {gameObject.name} 的 EnemyConfigSO 未配置！", this);
        return;
      }
      enemyData = new EnemyData(new AttributeEnemyBase(
          enemyConfig.maxHealth, enemyConfig.maxHealth,
          enemyConfig.attackPower, enemyConfig.moveSpeed));
      colliders = GetComponentsInChildren<Collider>();
      if (_network == null)
        _network = GetComponent<NetworkIdentityBase>();

      if (rb == null)
        rb = GetComponent<Rigidbody>();
      if (animator == null)
        animator = GetComponent<Animator>();
    }

    protected virtual void Start()
    {
      InitializeEnemy();
    }

    // 初始化方法，从池中取出时也会调用
    protected virtual void InitializeEnemy()
    {
      if (!enemyData.isInitialized)
      {
        enemyData.isInitialized = true;
      }
      TransitionToState(EnemyState.Idle);
    }

    protected virtual void Update()
    {
      if (GamePauseManager.isPaused)
        return;
      // 如果已死亡，不执行更新逻辑
      if (enemyData.currentState == EnemyState.Die)
      {
        return;
      }

      // 只有拥有者（Master Client）执行AI逻辑和移动
      // 其他客户端通过Photon网络同步获取位置和动画
      // 测试模式下无Photon网络，直接执行
      if (!EventChannelLocator.MainContainer.gameSettings.IsTest && !_network.IsMine)
      {
        return;
      }

      switch (enemyData.currentState)
      {
        case EnemyState.Idle:
          UpdateIdle();
          break;
        case EnemyState.Run:
          UpdateRun();
          break;
        case EnemyState.Attack:
          UpdateAttack();
          break;
        case EnemyState.Die:
          UpdateDie();
          break;
      }
    }

    //追踪最近的玩家
    protected virtual void TrackPlayer()
    {
      if (GetIsDie() || enemyData.currentState == EnemyState.Die)
      {
        // 停止物理移动
        if (rb != null)
        {
          rb.velocity = Vector3.zero;
          rb.angularVelocity = Vector3.zero;
        }
        return;
      }

      // 从 PlayerManager 获取缓存的玩家列表，避免每帧 FindGameObjectsWithTag
      IReadOnlyList<GameObject> players =
          PlayerManager.instance != null ? PlayerManager.instance.ActivePlayerObjects : null;

      if (players == null || players.Count == 0)
      {
        PlayerTarget = null;
        return;
      }

      PlayerTarget = players[0];
      for (int i = 1; i < players.Count; i++)
      {
        if (players[i] == null)
          continue;
        if (
            PlayerTarget == null
            || Vector3.Distance(transform.position, players[i].transform.position)
                < Vector3.Distance(transform.position, PlayerTarget.transform.position)
        )
        {
          PlayerTarget = players[i];
        }
      }
    }

    #region 状态机相关代码
    protected virtual void TransitionToState(EnemyState newState)
    {
      // 退出当前状态
      switch (enemyData.currentState)
      {
        case EnemyState.Idle:
          ExitIdle();
          break;
        case EnemyState.Run:
          ExitRun();
          break;
        case EnemyState.Attack:
          ExitAttack();
          break;
        case EnemyState.Die:
          ExitDie();
          break;
      }

      enemyData.currentState = newState;

      // 进入新状态
      switch (enemyData.currentState)
      {
        case EnemyState.Idle:
          EnterIdle();
          break;
        case EnemyState.Run:
          EnterRun();
          break;
        case EnemyState.Attack:
          EnterAttack();
          break;
        case EnemyState.Die:
          EnterDie();
          break;
      }
    }

    // ========== Idle状态 ==========
    protected virtual void EnterIdle()
    {
      TrackPlayer();
      if (animator != null)
      {
        animator.SetBool("isRun", false);
      }
    }

    protected virtual void UpdateIdle()
    {
      if (PlayerTarget)
      {
        TransitionToState(EnemyState.Run);
      }
      else
      {
        TrackPlayer();
        return;
      }
    }

    protected virtual void ExitIdle() { }

    // ========== Run状态 ==========
    protected virtual void EnterRun()
    {
      if (animator != null)
      {
        animator.SetBool("isRun", true);
      }
    }

    protected virtual void UpdateRun()
    {
      // 重新评估目标，确保已被移除的死亡玩家不再被追踪
      TrackPlayer();

      if (!PlayerTarget)
      {
        TransitionToState(EnemyState.Idle);
      }
      else
      {
        // 计算移动向量
        Vector3 moveDirection = (
            PlayerTarget.transform.position - transform.position
        ).normalized;
        // 移动敌人
        if (rb != null)
        {
          rb.MovePosition(
              transform.position + moveDirection * enemyData.attribute.moveSpeed * UnityEngine.Time.deltaTime
          );
        }
        // 旋转敌人朝向玩家,只在xz轴上旋转
        if (PlayerTarget != null)
        {
          transform.LookAt(
              new Vector3(
                  PlayerTarget.transform.position.x,
                  transform.position.y,
                  PlayerTarget.transform.position.z
              )
          );
        }
      }
    }

    protected virtual void ExitRun()
    {
      if (animator != null)
      {
        animator.SetBool("isRun", false);
      }
    }

    // ========== Attack状态 ==========
    protected virtual void EnterAttack() { }

    protected virtual void UpdateAttack() { }

    protected virtual void ExitAttack() { }

    // ========== Die状态 ==========
    protected virtual void EnterDie()
    {
      EventChannelLocator.MainContainer.enemyReportChannel.Raise(new EnemyReportData(transform.position, _network.ViewID));

      // 记录本次击杀到对局统计
      MatchStatisticsManager.Instance?.RecordKill();

      // 停止物理移动
      if (rb != null)
      {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
      }

      // 停止 NavMeshAgent（所有怪物类型通用，即使子类不继承 AttackableEnemy）
      var navAgent = GetComponent<NavMeshAgent>();
      if (navAgent != null && navAgent.isActiveAndEnabled)
      {
        navAgent.isStopped = true;
        if (navAgent.isOnNavMesh)
          navAgent.ResetPath();
      }

      // 禁用所有 Collider（所有客户端都执行）
      // 注意：之前非房主端不禁用 Collider 是因为 AttackRange 依赖 OnTriggerStay 检测死亡，
      // 现在已经改用 IsDeadOrDying() 方法通过状态机判断死亡，不再依赖 Collider 状态
      foreach (Collider collider in colliders)
      {
        collider.enabled = false;
      }

      if (animator != null)
      {
        animator.SetTrigger("die");
        animator.Update(0f); // 强制立即触发动画过渡，修复非房主端死亡动画不播放的问题
      }

      DropExperience();

      // 使用对象池回收而不是直接销毁
      Invoke(nameof(ReturnToPool), 3f);
    }

    protected virtual void UpdateDie() { }

    protected virtual void ExitDie() { }
    #endregion

    // 返回对象池（子类可重写以指定池名）
    protected virtual void ReturnToPool()
    {
      if (EventChannelLocator.MainContainer.gameSettings.IsTest)
      {
        // 测试模式：直接本地回池
        EventChannelLocator.MainContainer.poolOperationChannel.Raise(
            PoolOperationData.CreateDespawn(GetPoolName(), gameObject));
        return;
      }

      if (_network.IsMasterClient)
      {
        // 房主端：PhotonNetwork.Destroy 同步到所有客户端
        EventChannelLocator.MainContainer.poolOperationChannel.Raise(
            PoolOperationData.CreateDespawn(GetPoolName(), gameObject));
      }
      else
      {
        // 非房主端：通知房主销毁此怪物（处理房主端未检测到死亡的情况）
        NetworkServiceLocator.DomainRpcService?.InvokeRPC(
            "RPC_RequestEnemyDestroy",
            NetworkTarget.MasterClient,
            _network.ViewID);
        // 本地立即禁用，不等网络同步
        gameObject.SetActive(false);
      }
    }

    // 获取对象池名称（子类重写以指定自己所属的池）
    protected virtual string GetPoolName()
    {
      return PoolConst.Skeleton;
    }

    public virtual EnemyState GetCurrentState()
    {
      return enemyData.currentState;
    }

    public virtual bool GetIsDie()
    {
      // ★ 非房主客户端：不依赖本地死亡状态（isDead），避免 AttackRange 误判后移除目标
      // 非房主只负责显示伤害数字和扣血效果，不参与死亡判定
      // 房主销毁怪物时 PhotonNetwork.Destroy 会同步到所有客户端，届时怪物自然被移除
      if (!EventChannelLocator.MainContainer.gameSettings.IsTest
          && _network != null
          && !_network.IsMasterClient)
      {
        return false;
      }
      return enemyData.attribute.GetIsDie();
    }

    /// <summary>
    /// 判断敌人是否真正死亡（对所有客户端生效）
    /// 与 GetIsDie() 的区别：非房主端也会检查状态机，适用于弹道/投射物的死亡检测
    /// </summary>
    public bool IsDeadOrDying()
    {
      return GetIsDie() || enemyData.currentState == EnemyState.Die;
    }

    protected virtual void OnEnable()
    {
      // 从对象池取出时重新初始化
      if (enemyData.isInitialized)
      {
        // [修复-问题四] 调用 ResetState 确保对象池回收后的状态被完全重置
        // 包括：物理(isKinematic)/碰撞体/动画/属性(isDead/currentHealth)/目标等
        ResetState();

        // 确保属性不为 null（[NonSerialized] 字段在序列化后可能丢失）
        if (enemyData.attribute == null)
        {
          enemyData.attribute = new AttributeEnemyBase(
              enemyConfig.maxHealth, enemyConfig.maxHealth,
              enemyConfig.attackPower, enemyConfig.moveSpeed);
        }

        InitializeEnemy();
      }

      // [修复] 每次 OnEnable 都重新获取事件通道，避免第一次获取失败后永久缓存 null
      damageEventChannel = EventChannelLocator.MainContainer?.damageEventChannel;
      if (damageEventChannel != null)
      {
        damageEventChannel.RegisterListener(OnDamageReceived);
      }
      else
      {
        Debug.LogWarning($"[EnemyBase] {gameObject.name} 无法获取 damageEventChannel，将无法收到伤害事件", this);
      }
    }

    protected virtual void OnDisable()
    {
      // 取消所有Invoke调用
      CancelInvoke();

      // 停止所有协程
      StopAllCoroutines();
      damageEventChannel?.UnregisterListener(OnDamageReceived);
    }


    protected virtual void OnDamageReceived(EventArgsBase args)
    {
      // 空引用检查：确保 attribute 不为 null
      if (enemyData.attribute == null)
      {
        Debug.LogWarning(
            $"[EnemyBase] OnDamageReceived: attribute 为空，敌人对象: {gameObject.name}"
        );
        return;
      }

      if (enemyData.attribute.GetIsDie())
      {
        return;
      }
      DamageEventArgs damageEventArgs = args as DamageEventArgs;
      if (damageEventArgs == null)
      {
        Debug.LogWarning($"期望 DamageEventArgs，但收到 {args?.GetType()}");
        return;
      }
      if (damageEventArgs.damgeTarget != gameObject)
      {
        return;
      }
      TakeDamage(damageEventArgs);
    }

    public virtual void TakeDamage(DamageEventArgs damageEventArgs)
    {
      if (enemyData.attribute.GetIsDie())
      {
        return;
      }
      damageEventArgs.CalculateFinalDamageValue();
      damageEventArgs.finalDamageValue = Mathf.Ceil(damageEventArgs.finalDamageValue);
      enemyData.attribute.TakeDamage(damageEventArgs.finalDamageValue);

      // 记录本次伤害到对局统计
      MatchStatisticsManager.Instance?.RecordDamage(Mathf.RoundToInt(damageEventArgs.finalDamageValue));
      QuestTaskManager.Instance?.RecordDamage(Mathf.RoundToInt(damageEventArgs.finalDamageValue));

      Debug.Log(
          "最终伤害为："
              + damageEventArgs.finalDamageValue
              + "是否死亡:"
              + enemyData.attribute.GetIsDie()
      );
      if (EventChannelLocator.MainContainer.gameSettings.IsTest)
      {
        EventChannelLocator.MainContainer.matchSystem.damageDisplayChannel?.Raise(
            DamageDisplayEventArgs.GetShared(
                damageEventArgs.finalDamageValue,
                transform.position,
                damageEventArgs.isCritical
            )
        );
      }
      enemyData.attribute.TakeDamageSpecial(damageEventArgs.element);

      if (enemyData.attribute.GetIsDie())
      {
        TransitionToState(EnemyState.Die);
      }
    }

    //死亡后掉落经验
    protected virtual void DropExperience()
    {
      Debug.Log("敌人死亡，掉落经验");

      // 道具掉落概率从 SO 配置读取
      float dropChance = enemyConfig.powerUpDropChance;
      if (Random.value <= dropChance && PowerUpManager.Instance != null)
      {
        bool isTest = EventChannelLocator.MainContainer.gameSettings.IsTest;
        if (isTest)
        {
          // 测试模式：直接本地生成
          PowerUpManager.Instance.SpawnRandomPowerUp(transform.position);
        }
        else if (NetworkServiceLocator.PlayerService.IsMasterClient)
        {
          // 联机模式：仅房主生成 itemId 并广播 RPC 到所有客户端
          PowerUpManager.Instance.SpawnRandomPowerUp(transform.position);
        }
        Debug.Log("[PowerUp] 敌人掉落道具！");
      }
    }

    /// <summary>从 SO 配置获取随机经验值（供子类 DropExperience 调用）</summary>
    protected int GetExpValue()
    {
      return Random.Range(enemyConfig.expMin, enemyConfig.expMax + 1);
    }

    // 重置状态（对象池回收时调用）
    public virtual void ResetState()
    {
      // 恢复刚体状态
      if (rb != null)
      {
        rb.isKinematic = false;
      }

      foreach (Collider collider in colliders)
      {
        collider.enabled = true;
      }

      // 1. 取消所有Invoke和协程
      CancelInvoke();
      StopAllCoroutines();

      // 2. 重置状态机
      if (enemyData.currentState == EnemyState.Die)
      {
        CancelInvoke(nameof(ReturnToPool));
      }
      enemyData.currentState = EnemyState.Idle;

      // 3. 重置动画
      if (animator != null)
      {
        animator.ResetTrigger("die");
        animator.SetBool("isRun", false);
        animator.Rebind(); // 重置所有动画状态
        animator.Update(0f); // 立即更新动画
      }

      // 4. 重置物理
      if (rb != null)
      {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
      }

      // 5. 重置属性
      if (enemyData.attribute != null)
      {
        enemyData.attribute.ResetAttribute();
      }

      // 6. 清除目标
      PlayerTarget = null;

      // 7. 重置数据类的运行时状态
      enemyData?.ResetRuntimeState();
    }
  }
}