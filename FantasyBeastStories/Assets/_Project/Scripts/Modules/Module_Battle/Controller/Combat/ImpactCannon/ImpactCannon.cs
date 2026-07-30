using System.Collections;
using System.Collections.Generic;
using Controllers.Character;
using Core;
using Core.Channels.Combat;
using Core.Channels.Player;
using Controllers.Network;
using Controllers.Battle;
using UnityEngine;
using Core.Audio;
using Core.SharedModel;

namespace Controllers.Battle.ImpactCannon
{
  /// <summary>
  /// ImpactCannon 投射物 — 直线飞行 + 穿透 + 分裂。
  /// Phase 6: 改为继承 ProjectileBase，复用公共字段和 GC 优化。
  /// </summary>
  public class ImpactCannon : ProjectileBase, IImpactCannon
  {
    [Header("事件通道")]
    [SerializeField] private DamageEventChannelSO damageEventChannel;

    private int maxAttackCount = 1;
    private int attackCount = 0;
    private AttributePlayerBase attributePlayer;
    private float Speed = 15f;
    private Rigidbody rb;
    private float damageFalloff = 1f;

    private Vector3 baseScale;

    [SerializeField]
    private float splitRange = 20f;
    [SerializeField]
    private float splitAngle = 30f;

    private GameObject ignoreEnemy;

    private bool isTest;

    protected override void Awake()
    {
      base.Awake();
      isTest = EventChannelLocator.MainContainer != null && EventChannelLocator.MainContainer.gameSettings != null && EventChannelLocator.MainContainer.gameSettings.IsTest;
      rb = GetComponent<Rigidbody>();
      if (_castNetwork == null)
        _castNetwork = FindObjectOfType<CastNetwork>();
      baseScale = transform.localScale;

      damageEventChannel = EventChannelLocator.MainContainer?.damageEventChannel;
      if (damageEventChannel == null)
        Debug.LogWarning("[ImpactCannon] damageEventChannel 未配置");
    }

    public void OnEnable()
    {
      var query = new SkillQueryData(SkillQueryType.GetMaxAttackCount);
      EventChannelLocator.MainContainer.skillQueryChannel.Raise(query);
      maxAttackCount = query.intValue;
      attackCount = 0;
      ignoreEnemy = null;
      _canSplit = true;
      damageFalloff = 1f;
      if (attributePlayer?.GetSplit() != null && attributePlayer?.GetSplitCount() != null)
      {
        _canSplit = attributePlayer.GetSplit();
        _splitCount = attributePlayer.GetSplitCount();
      }
      else
      {
        _canSplit = false;
      }
      StartCoroutine(DelayDestroySelf(0.5f));
    }

    void OnDisable()
    {
      transform.localScale = baseScale;
      rb.velocity = Vector3.zero;
      StopAllCoroutines();
    }

    public void SetAttributeFromPlayer(AttributePlayerBase attributePlayer)
    {
      this.attributePlayer = attributePlayer;
      _element = attributePlayer.GetCurrentElement();
      _canSplit = attributePlayer.GetSplit();
      _splitCount = attributePlayer.GetSplitCount();
    }

    /// <summary>供远程客户端设置元素类型（无需 attributePlayer）</summary>
    public void SetElement(Element element) => _element = element;

    public void SetCanSplit(bool value) => _canSplit = value;

    public void StartShoot(Vector3 direction, bool isMine = true)
    {
      if (!EventChannelLocator.MainContainer.gameSettings.IsTest)
      {
        _isMine = isMine;
      }
      direction.y = 0;
      rb.velocity = direction.normalized * Speed;
    }

    public override void OnTriggerEnter(Collider other)
    {
      base.OnTriggerEnter(other);

      if (ignoreEnemy != null && other.gameObject == ignoreEnemy)
        return;

      if (!other.CompareTag("Enemy"))
        return;

      Vector3 hitPoint = other.ClosestPoint(transform.position);

      if (_isMine)
      {
        if (attributePlayer == null)
        {
          Debug.LogWarning("[ImpactCannon] attributePlayer 为空");
          return;
        }

        if (_canSplit)
        {
          SplitToNearestEnemies(hitPoint, other.gameObject);
          _canSplit = false;
        }

        PlayHitEffect(hitPoint);
        attackCount++;

        bool isCritical = UnityEngine.Random.Range(0, 1f) <= attributePlayer.GetCriticalChance();
        float damage = attributePlayer.GetAttackPower() * damageFalloff;

        if (isTest)
        {
          DealDamageLocal(other.gameObject, damage, isCritical, attributePlayer.GetCriticalMultiplier());
        }
        else
        {
          AudioManager.Instance.PlaySFX("sfx_wizard_hit", hitPoint);
          _castNetwork?.BroadcastDamage(
              other.gameObject, damage, isCritical,
              attributePlayer.GetCriticalMultiplier(), hitPoint,
              attributePlayer.GetCurrentElement()
          );
        }

        if (attackCount >= maxAttackCount)
        {
          RecycleWithEffect();
          return;
        }
      }
      else
      {
        PlayHitEffect(hitPoint);
        attackCount++;
      }

      if (attackCount >= maxAttackCount)
        RecycleWithEffect();
    }

    protected override void SplitToNearestEnemies(Vector3 hitPoint, GameObject hitEnemy)
    {
      Collider[] enemiesInRange = Physics.OverlapSphere(hitPoint, splitRange, LayerMask.GetMask("Enemy"));

      _validTargetsCache.Clear();
      foreach (var col in enemiesInRange)
      {
        if (col.gameObject != hitEnemy)
          _validTargetsCache.Add(col);
      }

      _sortOrigin = hitPoint;
      _validTargetsCache.Sort(_sortByDistance);
      int actualSplitCount = Mathf.Min(_splitCount, _validTargetsCache.Count);
      for (int i = 0; i < actualSplitCount; i++)
      {
        Vector3 targetPos = _validTargetsCache[i].transform.position;
        Vector3 xzTargetPos = new Vector3(targetPos.x, hitPoint.y, targetPos.z);
        Vector3 baseDirection = (xzTargetPos - hitPoint).normalized;
        Vector3 splitDirection = GetSplitDirection(baseDirection, i, actualSplitCount);
        CreateSplitBullet(hitPoint, splitDirection, _validTargetsCache[i].gameObject, hitEnemy);
      }
    }

    private Vector3 GetSplitDirection(Vector3 baseDirection, int index, int total)
    {
      if (total <= 1) return baseDirection;
      float halfAngle = splitAngle / 2f;
      float step = total > 1 ? splitAngle / (total - 1) : 0;
      float currentAngle = -halfAngle + step * index;
      return Quaternion.Euler(0, currentAngle, 0) * baseDirection;
    }

    private void CreateSplitBullet(Vector3 spawnPos, Vector3 direction, GameObject targetEnemy, GameObject ignoreEnemyObj = null)
    {
      string poolName = AttackRangeBase.GetImpactCannonPoolByElement(attributePlayer.GetCurrentElement());

      GameObject visualObj = PoolHelper.Get(poolName, spawnPos);
      GameObject triggerObj = PoolHelper.Get(PoolConst.ImpactCannonTriggerPool, spawnPos);

      if (visualObj == null || triggerObj == null)
      {
        Debug.LogWarning("无法从对象池获取分裂弹组件");
        if (visualObj != null) PoolHelper.Return(poolName, visualObj);
        if (triggerObj != null) PoolHelper.Return(PoolConst.ImpactCannonTriggerPool, triggerObj);
        return;
      }

      if (visualObj != null)
      {
        var particle = visualObj.GetComponentInChildren<ParticleSystem>();
        particle?.Play();
        visualObj.transform.rotation = Quaternion.LookRotation(direction);
      }

      ImpactCannon splitCannon = triggerObj.GetComponent<ImpactCannon>();
      if (splitCannon != null)
      {
        AttackToken splitToken = new AttackToken
        {
          hitCollider = triggerObj,
          vfxEffect = visualObj,
          vfxPoolName = poolName,
        };
        splitCannon.SetToken(splitToken);
        splitCannon.StartShoot(direction, true);
        splitCannon.SetAttributeFromPlayer(attributePlayer);
        splitCannon.ignoreEnemy = ignoreEnemyObj;
        splitCannon._canSplit = false;
        splitCannon.damageFalloff = 0.5f;
      }

      if (!isTest && _castNetwork != null)
      {
        _castNetwork.RequestSplitBullet(spawnPos, direction, (int)attributePlayer.GetCurrentElement());
      }
    }

    private void PlayHitEffect(Vector3 hitPosition)
    {
      string poolKey = AttackRangeBase.GetImpactCannonHitPoolByElement(_element);
      GameObject hitEffect = PoolHelper.Get(poolKey, hitPosition);
      if (hitEffect != null)
        hitEffect.GetComponentInChildren<ParticleSystem>()?.Play();
    }

    private void DealDamageLocal(GameObject enemyObj, float damage, bool isCritical, float criticalMultiplier, Element element = Element.Common)
    {
      DamageEventArgs damageEventArgs = DamageEventArgs.GetShared(element, gameObject, enemyObj, damage, isCritical, criticalMultiplier);
      var channel = EventChannelLocator.MainContainer?.damageEventChannel;
      channel?.Raise(damageEventArgs);
    }

    private IEnumerator DelayDestroySelf(float delay)
    {
      yield return new WaitForSeconds(delay);
      PoolHelper.Return(PoolConst.ImpactCannonTriggerPool, gameObject);
    }

    void OnDrawGizmos()
    {
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

    private AttackToken token;

    public void SetToken(AttackToken newToken) => token = newToken;

    private void RecycleWithEffect()
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
        PoolHelper.Return(PoolConst.ImpactCannonTriggerPool, gameObject);
      }
    }
  }
}
