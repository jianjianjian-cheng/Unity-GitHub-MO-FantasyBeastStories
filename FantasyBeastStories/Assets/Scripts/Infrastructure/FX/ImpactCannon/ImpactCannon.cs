using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using Domain.Character.Attribute;
using Domain.Character;
using Domain.Event;
using Domain.Event.Channels.Combat;
using Domain.Event.Channels.Player;
using Domain.Pool;
using Infrastructure.Network;
using Domain.Combat.Trigger;
using Domain.Combat.FX;
using Unity.VisualScripting;
using UnityEngine;
using Domain.Data;

namespace Infrastructure.FX.ImpactCannon
{
  public class ImpactCannon : TriggerBase, IImpactCannon
  {

    [Header("事件通道")]
    [SerializeField] private DamageEventChannelSO damageEventChannel;

    // true  → 我是发射者，我负责判定伤害
    // false → 我是别人的火球，我只负责视觉表现
    private bool _isMyCast = true;
    private CastNetwork _networkCaster;

    [SerializeField]
    private bool isTest;
    private int maxAttackCount = 1;
    private int attackCount = 0;
    private AttributePlayerBase attributePlayer;
    private float Speed = 15f;
    private Rigidbody rb;
    private float damageFalloff = 1f; // 伤害衰减系数

    private Vector3 baseScale;

    public bool isSplit = true;

    [SerializeField]
    private float splitRange = 20f; // 搜索敌人的范围
    private int splitCount = 2;
    public bool canSplit = true;
    private GameObject ignoreEnemy;

    [SerializeField]
    private float splitAngle = 30f; // 分裂角度范围

    [SerializeField]
    private float splitDamageMultiplier = 0.5f; // 分裂弹伤害倍率

    void Awake()
    {
      isTest = EventChannelLocator.MainContainer != null && EventChannelLocator.MainContainer.gameSettings != null && EventChannelLocator.MainContainer.gameSettings.IsTest;
      rb = GetComponent<Rigidbody>();
      _networkCaster = FindObjectOfType<CastNetwork>();
      baseScale = transform.localScale;

      // [修复] 每次 Awake 都重新获取事件通道，避免缓存 null
      damageEventChannel = EventChannelLocator.MainContainer?.damageEventChannel;
      if (damageEventChannel == null)
      {
        Debug.LogWarning("[ImpactCannon] damageEventChannel 未配置，请在Inspector中赋值或检查 MainContainer 是否就绪");
      }
    }

    public void OnEnable()
    {
      var query = new SkillQueryData(SkillQueryType.GetMaxAttackCount);
      EventChannelLocator.MainContainer.skillQueryChannel.Raise(query);
      maxAttackCount = query.intValue;
      attackCount = 0;
      ignoreEnemy = null;
      canSplit = true;
      damageFalloff = 1f;
      // 初始化分裂属性：只有玩家装备了分裂技能时，才能分裂
      if (attributePlayer?.GetSplit() != null && attributePlayer?.GetSplitCount() != null)
      {
        isSplit = attributePlayer.GetSplit();
        splitCount = attributePlayer.GetSplitCount();
        canSplit = isSplit; // 根据玩家技能决定是否允许分裂
      }
      else
      {
        canSplit = false; // 没有分裂技能时，不允许分裂
      }
      StartCoroutine(DelayDestroySelf(0.5f));
    }

    void OnDisable()
    {
      transform.localScale = baseScale;
      Debug.Log("冲击炮被禁用，返回对象池");
      rb.velocity = Vector3.zero;
      StopAllCoroutines();
    }

    public void SetAttributeFromPlayer(AttributePlayerBase attributePlayer)
    {
      this.attributePlayer = attributePlayer;
    }

    /// <summary>
    /// 发射物体
    /// </summary>
    /// <param name="direction">发射方向</param>
    /// <param name="isMine">是否由本地炮塔发射</param>
    // 修改后
    public void StartShoot(Vector3 direction, bool isMine = true)
    {
      if (!EventChannelLocator.MainContainer.gameSettings.IsTest)
      {
        _isMyCast = isMine;
      }
      direction.y = 0;
      rb.velocity = direction.normalized * Speed;
    }

    public override void Start()
    {
      base.Start();
    }

    public override void Update()
    {
      base.Update();
    }

    public override void OnTriggerEnter(Collider other)
    {
      base.OnTriggerEnter(other);

      // 防御性检查
      if (attributePlayer == null)
      {
        Debug.LogWarning("[ImpactCannon] attributePlayer 为空");
        return;
      }

      // 检查是否是需要忽略的敌人
      if (ignoreEnemy != null && other.gameObject == ignoreEnemy)
      {
        return;
      }

      if (!other.CompareTag("Enemy"))
        return;

      Vector3 hitPoint = other.ClosestPoint(transform.position);

      // ===== 只有本地玩家创建的火球（包括分裂弹）才判定伤害 =====
      if (_isMyCast)
      {
        Debug.Log("触发冲击炮击中效果");

        // 只有初始火球才能分裂（分裂弹的 canSplit = false）
        if (canSplit)
        {
          SplitToNearestEnemies(hitPoint, other.gameObject);
          canSplit = false;
        }

        PlayHitEffect(hitPoint);
        attackCount++;

        bool isCritical = Random.Range(0, 1f) <= attributePlayer.GetCriticalChance();
        float damage = attributePlayer.GetAttackPower() * damageFalloff; // 分裂弹伤害减半

        if (isTest)
        {
          DealDamageLocal(
              other.gameObject,
              damage,
              isCritical,
              attributePlayer.GetCriticalMultiplier()
          );
        }
        else
        {
          _networkCaster?.BroadcastDamage(
              other.gameObject,
              damage,
              isCritical,
              attributePlayer.GetCriticalMultiplier(),
              hitPoint,
              attributePlayer.GetCurrentElement()
          );
        }

        if (attackCount >= maxAttackCount)
        {
          RecycleWithEffect();
        }
      }
      else
      {
        // 其他玩家的火球：只播放视觉效果
        PlayHitEffect(hitPoint);
        attackCount++;  // 也需要计数
      }

      // ===== 所有客户端都检查穿透次数 =====
      if (attackCount >= maxAttackCount)
      {
        RecycleWithEffect();
      }
    }

    private void SplitToNearestEnemies(Vector3 hitPoint, GameObject hitEnemy)
    {
      // 1. 查找范围内所有敌人
      Collider[] enemiesInRange = Physics.OverlapSphere(
          hitPoint,
          splitRange,
          LayerMask.GetMask("Enemy")
      );

      // 2. 按距离排序（排除已命中的敌人）
      List<Collider> validTargets = new List<Collider>();
      foreach (var col in enemiesInRange)
      {
        if (col.gameObject != hitEnemy)
          validTargets.Add(col);
      }

      Debug.LogWarning($"找到{validTargets.Count}个敌人");
      // 按距离从近到远排序
      validTargets.Sort(
          (a, b) =>
              Vector3
                  .Distance(hitPoint, a.transform.position)
                  .CompareTo(Vector3.Distance(hitPoint, b.transform.position))
      );
      int actualSplitCount = Mathf.Min(splitCount, validTargets.Count);
      for (int i = 0; i < actualSplitCount; i++)
      {
        Vector3 targetPos = validTargets[i].transform.position;

        // 计算基础方向，只取xz轴方向
        Vector3 xzTargetPos = new Vector3(targetPos.x, hitPoint.y, targetPos.z);
        Vector3 baseDirection = (xzTargetPos - hitPoint).normalized;

        // 添加扇形偏移（让分裂弹看起来更自然）
        Vector3 splitDirection = GetSplitDirection(baseDirection, i, actualSplitCount);

        CreateSplitBullet(hitPoint, splitDirection, validTargets[i].gameObject, hitEnemy);
      }
    }

    /// <summary>
    /// 获取带扇形偏移的方向
    /// </summary>
    private Vector3 GetSplitDirection(Vector3 baseDirection, int index, int total)
    {
      if (total <= 1)
        return baseDirection;

      // 计算偏移角度（均匀分布在扇形内）
      float halfAngle = splitAngle / 2f;
      float step = total > 1 ? splitAngle / (total - 1) : 0;
      float currentAngle = -halfAngle + step * index;

      // 绕Y轴旋转方向
      return Quaternion.Euler(0, currentAngle, 0) * baseDirection;
    }

    /// <summary>
    /// 创建分裂弹
    /// </summary>
    private void CreateSplitBullet(
        Vector3 spawnPos,
        Vector3 direction,
        GameObject targetEnemy,
        GameObject ignoreEnemyObj = null
    )
    {
      string poolName = "";
      switch (attributePlayer.GetCurrentElement())
      {
        case Element.Common:
          poolName = ObjectPoolConst.ImpactCannonCommonPool;
          break;
        case Element.Lightning:
          poolName = ObjectPoolConst.ImpactCannonLightenPool;
          break;
        case Element.Winter:
          poolName = ObjectPoolConst.ImpactCannonWinterPool;
          break;
        case Element.Grass:
          poolName = ObjectPoolConst.ImpactCannonGrassPool;
          break;
        default:
          poolName = ObjectPoolConst.ImpactCannonCommonPool;
          break;
      }

      // 1. 获取视觉特效
      GameObject visualObj = null;
      EventChannelLocator.MainContainer.poolOperationChannel.Raise(
          PoolOperationData.CreateGet(poolName, spawnPos, (o) => visualObj = o));

      GameObject triggerObj = null;
      EventChannelLocator.MainContainer.poolOperationChannel.Raise(
          PoolOperationData.CreateGet(ObjectPoolConst.ImpactCannonTriggerPool, spawnPos, (o) => triggerObj = o));

      if (visualObj == null || triggerObj == null)
      {
        Debug.LogWarning("无法从对象池获取分裂弹组件");
        if (visualObj != null)
          EventChannelLocator.MainContainer.poolOperationChannel.Raise(
              PoolOperationData.CreateReturn(poolName, visualObj));
        if (triggerObj != null)
          EventChannelLocator.MainContainer.poolOperationChannel.Raise(
              PoolOperationData.CreateReturn(ObjectPoolConst.ImpactCannonTriggerPool, triggerObj));
        return;
      }

      // 4. 设置视觉特效
      if (visualObj != null)
      {
        var particle = visualObj.GetComponentInChildren<ParticleSystem>();
        particle?.Play();
        visualObj.transform.rotation = Quaternion.LookRotation(direction);
      }

      // 5. 设置碰撞触发器
      ImpactCannon splitCannon = triggerObj.GetComponent<ImpactCannon>();
      if (splitCannon != null)
      {
        // 创建令牌并绑定视觉特效和触发器
        AttackToken splitToken = new AttackToken
        {
          hitCollider = triggerObj,
          vfxEffect = visualObj,
          vfxPoolName = poolName,
        };

        splitCannon.SetToken(splitToken);
        splitCannon.StartShoot(direction, true);
        splitCannon.SetAttributeFromPlayer(attributePlayer);
        splitCannon.isSplit = false; // 防止无限分裂
        splitCannon.ignoreEnemy = ignoreEnemyObj;
        splitCannon.canSplit = false;
        splitCannon.damageFalloff = 0.5f;
      }
      if (!isTest && _networkCaster != null)
      {
        _networkCaster.RequestSplitBullet(
            spawnPos,
            direction,
            (int)attributePlayer.GetCurrentElement()
        );
      }
    }

    /// <summary>
    /// 播放命中特效
    /// </summary>
    private void PlayHitEffect(Vector3 hitPosition)
    {
      string poolKey = "";
      switch (attributePlayer.GetCurrentElement())
      {
        case Element.Common:
          poolKey = ObjectPoolConst.ImpactCannonHitCommonPool;
          break;
        case Element.Lightning:
          poolKey = ObjectPoolConst.ImpactCannonHitLightenPool;
          break;
        case Element.Winter:
          poolKey = ObjectPoolConst.ImpactCannonHitWinterPool;
          break;
        case Element.Grass:
          poolKey = ObjectPoolConst.ImpactCannonHitGrassPool;
          break;
        default:
          poolKey = ObjectPoolConst.ImpactCannonHitCommonPool;
          break;
      }
      GameObject hitEffect = null;
      EventChannelLocator.MainContainer.poolOperationChannel.Raise(
          PoolOperationData.CreateGet(poolKey, hitPosition, (o) => hitEffect = o));
      if (hitEffect != null)
      {
        hitEffect.GetComponentInChildren<ParticleSystem>()?.Play();
      }
    }

    /// <summary>
    /// 本地伤害处理（测试模式用）
    /// </summary>
    private void DealDamageLocal(
        GameObject enemyObj,
        float damage,
        bool isCritical,
        float criticalMultiplier,
        Element element = Element.Common
    )
    {
      DamageEventArgs damageEventArgs = new DamageEventArgs(
          element,
          gameObject,
          enemyObj,
          damage,
          isCritical,
          criticalMultiplier
      );

      // 使用事件通道触发伤害（不缓存，每次都重新获取）
      var channel = EventChannelLocator.MainContainer?.damageEventChannel;
      channel?.Raise(damageEventArgs);
      if (channel == null)
      {
        Debug.LogWarning($"[ImpactCannon] damageEventChannel 为空，无法发送伤害事件", this);
      }
      Debug.Log($"[ImpactCannon] 通过事件通道触发伤害，目标: {enemyObj.name}");
    }

    public override void OnTriggerStay(Collider other)
    {
      base.OnTriggerStay(other);
    }

    public override void OnTriggerExit(Collider other)
    {
      base.OnTriggerExit(other);
    }

    //画出自身的范围
    void OnDrawGizmos()
    {
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

    private IEnumerator DelayDestroySelf(float delay)
    {
      yield return new WaitForSeconds(delay);
      EventChannelLocator.MainContainer.poolOperationChannel.Raise(
          PoolOperationData.CreateReturn(ObjectPoolConst.ImpactCannonTriggerPool, gameObject));
    }

    // 令牌 用于绑定特效
    private AttackToken token;

    public void SetToken(AttackToken newToken)
    {
      token = newToken;
    }

    void RecycleWithEffect()
    {
      StopAllCoroutines();

      if (token != null)
      {
        token.RecycleAll();
        token = null;
      }
      else
      {
        Debug.LogWarning("令牌丢失，无法回收所有特效");
        EventChannelLocator.MainContainer.poolOperationChannel.Raise(
            PoolOperationData.CreateReturn(ObjectPoolConst.ImpactCannonTriggerPool, gameObject));
      }
    }
  }
}