using Domain.Combat.FX;
using UnityEngine;
using Domain.Event;
using Domain.Data;
using Domain.Pool;
using Domain.Network;
using Application;

namespace Domain.Combat.Trigger
{
  /// <summary>
  /// 巫师男孩攻击范围：发射火球攻击
  /// </summary>
  public class AttackRange_WizardBoy : AttackRangeBase
  {

    private bool isTest = false;

    [Header("WizardBoy Settings")]
    [SerializeField]
    private float projectileSpeed = 10f;

    public override void Start()
    {
      base.Start();

      _networkCaster = ComponentFactory.GetOrCreateNetworkCaster(gameObject);
      isTest = EventChannelLocator.MainContainer != null && EventChannelLocator.MainContainer.gameSettings != null && EventChannelLocator.MainContainer.gameSettings.IsTest;

      if (_networkCaster == null && !isTest)
      {
        Debug.LogError("[AttackRange_WizardBoy] 无法获取或创建 INetworkFireballCaster，请检查 ComponentFactory 是否已注册");
      }
    }

    /// <summary>
    /// 实现具体的攻击逻辑：发射火球
    /// </summary>
    protected override void PerformAttack()
    {
      Vector3 pos = GetSpawnPosition();
      Vector3 direction = GetTargetDirection();

      if (isTest)
      {
        // 测试模式：纯本地生成
        SpawnFireballLocal(pos, direction, isMine: true);
      }
      else
      {
        // 联机模式：本地先行 + 网络广播
        SpawnFireballLocal(pos, direction, isMine: true);
        _networkCaster?.RequestFireball(
            pos,
            direction,
            projectileSpeed,
            attributePlayerBase.GetCurrentElement()
        );
      }
    }

    /// <summary>
    /// 纯本地生成发射物（视觉特效 + 碰撞触发器）
    /// </summary>
    private void SpawnFireballLocal(Vector3 spawnPos, Vector3 direction, bool isMine = true)
    {
      string visualPool = null;
      string triggerPool = ObjectPoolConst.ImpactCannonTriggerPool;
      AudioManager.Instance.PlaySFX("sfx_wizard_fire", spawnPos);
      // 1. 生成视觉特效
      switch (attributePlayerBase.GetCurrentElement())
      {
        case Element.Common:
          visualPool = ObjectPoolConst.ImpactCannonCommonPool;
          break;
        case Element.Lightning:
          visualPool = ObjectPoolConst.ImpactCannonLightenPool;
          break;
        case Element.Winter:
          visualPool = ObjectPoolConst.ImpactCannonWinterPool;
          break;
        case Element.Grass:
          visualPool = ObjectPoolConst.ImpactCannonGrassPool;
          break;
      }
      GameObject visualObj = null;
      EventChannelLocator.MainContainer.poolOperationChannel.Raise(
          PoolOperationData.CreateGet(visualPool, spawnPos, (o) => visualObj = o));

      GameObject triggerObj = null;
      EventChannelLocator.MainContainer.poolOperationChannel.Raise(
          PoolOperationData.CreateGet(triggerPool, spawnPos, (o) => triggerObj = o));

      // Vector3 baseScale = visualObj.transform.localScale;
      // Vector3 triggerScale = triggerObj.transform.localScale;
      // if (isCharged)
      // {
      //     visualObj.transform.localScale = baseScale * 1.5f;
      //     triggerObj.transform.localScale = triggerScale * 1.5f;
      // }
      // 创建令牌并绑定
      AttackToken token = new AttackToken
      {
        hitCollider = triggerObj,
        vfxEffect = visualObj,
        vfxPoolName = visualPool,
      };

      if (visualObj != null)
      {
        var particle = visualObj.GetComponentInChildren<ParticleSystem>();
        particle?.Play();
        visualObj.transform.rotation = Quaternion.LookRotation(direction);
      }

      if (triggerObj != null)
      {
        IImpactCannon cannon = ComponentFactory.GetOrCreateImpactCannon(triggerObj);
        if (cannon == null)
        {
          Debug.LogError("[AttackRange_WizardBoy] 无法获取或创建 IImpactCannon，请检查 ComponentFactory 是否已注册");
          return;
        }
        cannon.SetToken(token);
        cannon.SetAttributeFromPlayer(attributePlayerBase);
        cannon.StartShoot(direction, isMine);
      }
    }

    protected override void OnDrawGizmosSelected()
    {
      base.OnDrawGizmosSelected();
      // 可以添加WizardBoy特有的可视化
      if (targetEnemy != null)
      {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(targetEnemy.transform.position, 0.5f);
      }
    }
  }
}