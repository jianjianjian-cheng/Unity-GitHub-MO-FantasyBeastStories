using System.Collections;
using System.Collections.Generic;
using Domain.Event;
using Domain.Pool;
using Infrastructure.FX.ImpactCannon;
using Photon.Pun;
using Domain.Combat.Trigger;
using UnityEngine;
using Domain.Data;
using Domain.Network;
using Presentation.UI;

namespace Infrastructure.Network
{
  public class CastNetwork : MonoBehaviourPun, INetworkFireballCaster
  {
    /// <summary>
    /// 请求发射火球（由 AttackRangeBase 调用）
    /// 功能：向其他玩家广播发射指令
    /// </summary>
    public void RequestFireball(
        Vector3 spawnPos,
        Vector3 direction,
        float speed,
        Element element
    )
    {
      photonView.RPC(
          "RPC_OnFireballCast",
          RpcTarget.Others,
          spawnPos,
          direction,
          speed,
          (int)element
      );
    }

    /// <summary>
    /// RPC：其他玩家收到后，在本地生成火发射物
    /// </summary>
    [PunRPC]
    void RPC_OnFireballCast(Vector3 spawnPos, Vector3 direction, float speed, int elementInt)
    {
      Element element = (Element)elementInt;
      SpawnFireballForOthers(spawnPos, direction, element);
    }

    /// <summary>
    /// 为其他玩家生成本地火球（isMine = false，不负责伤害判定）
    /// </summary>
    private void SpawnFireballForOthers(Vector3 spawnPos, Vector3 direction, Element element)
    {
      // 1. 根据元素类型选择视觉特效池
      string visualPool = GetVisualPoolByElement(element);
      GameObject visualObj = null;
      EventChannelLocator.MainContainer.poolOperationChannel.Raise(
          PoolOperationData.CreateGet(visualPool, spawnPos, (o) => visualObj = o));
      if (visualObj != null)
      {
        visualObj.GetComponentInChildren<ParticleSystem>()?.Play();
        visualObj.transform.rotation = Quaternion.LookRotation(direction);
      }

      GameObject triggerObj = null;
      EventChannelLocator.MainContainer.poolOperationChannel.Raise(
          PoolOperationData.CreateGet(ObjectPoolConst.ImpactCannonTriggerPool, spawnPos, (o) => triggerObj = o));
      if (triggerObj != null)
      {
        ImpactCannon cannon = triggerObj.GetComponent<ImpactCannon>();
        if (cannon == null)
        {
          cannon = triggerObj.AddComponent<ImpactCannon>();
        }
        // 重要：传入 isMine = false，这个火球不会判定伤害
        //绑定token
        AttackToken token = new AttackToken
        {
          hitCollider = triggerObj,
          vfxEffect = visualObj,
          vfxPoolName = visualPool,
        };
        cannon.SetToken(token);
        cannon.StartShoot(direction, isMine: false);
      }
    }

    // ==================== 辅助方法 ====================

    /// <summary>
    /// 根据元素类型获取视觉特效池名
    /// </summary>
    private string GetVisualPoolByElement(Element element)
    {
      switch (element)
      {
        case Element.Lightning:
          return ObjectPoolConst.ImpactCannonLightenPool;
        case Element.Winter:
          return ObjectPoolConst.ImpactCannonWinterPool;
        default:
          return ObjectPoolConst.ImpactCannonCommonPool;
      }
    }

    /// <summary>
    /// 根据元素类型获取击中特效池名
    /// </summary>
    private string GetHitPoolByElement(Element element)
    {
      switch (element)
      {
        case Element.Lightning:
          return ObjectPoolConst.ImpactCannonHitLightenPool;
        case Element.Winter:
          return ObjectPoolConst.ImpactCannonHitWinterPool;
        case Element.Grass:
          return ObjectPoolConst.ImpactCannonHitGrassPool;
        default:
          return ObjectPoolConst.ImpactCannonHitCommonPool;
      }
    }

    // ==================== 伤害相关 ====================

    /// <summary>
    /// 广播伤害给所有客户端（由 ImpactCannon 调用）
    /// </summary>
    public void BroadcastDamage(
        GameObject enemyObj,
        float damage,
        bool isCritical,
        float criticalMultiplier,
        Vector3 hitPoint,
        Element element
    )
    {
      // 添加防御性检查
      if (this == null || gameObject == null)
      {
        Debug.LogWarning("[CastNetwork] 对象已销毁");
        return;
      }
      PhotonView enemyView = enemyObj.GetComponent<PhotonView>();
      if (enemyView == null)
        return;

      // 确保 photonView 有效
      if (photonView == null)
      {
        Debug.LogWarning("[CastNetwork] photonView 为空");
        return;
      }

      // 发给所有玩家（RpcTarget.All）
      photonView.RPC(
          "RPC_DealDamage",
          RpcTarget.All,
          enemyView.ViewID,
          damage,
          isCritical,
          criticalMultiplier,
          hitPoint,
          (int)element
      );
      photonView.RPC(
          "RPC_ShowDamageNum",
          RpcTarget.All,
          damage,
          hitPoint,
          isCritical,
          criticalMultiplier
      );
    }

    /// <summary>
    /// RPC：所有客户端执行扣血和特效
    /// </summary>
    [PunRPC]
    void RPC_DealDamage(
        int enemyViewID,
        float damage,
        bool isCritical,
        float criticalMultiplier,
        Vector3 hitPoint,
        int elementInt
    )
    {
      // 1. 通过 ViewID 找到敌人对象
      PhotonView enemyView = PhotonView.Find(enemyViewID);
      if (enemyView == null)
        return;

      // 2. 根据元素类型选择击中特效池
      Element element = (Element)elementInt;
      string poolKey = GetHitPoolByElement(element);
      GameObject hitEffect = null;
      EventChannelLocator.MainContainer.poolOperationChannel.Raise(
          PoolOperationData.CreateGet(poolKey, hitPoint, (o) => hitEffect = o));
      if (hitEffect != null)
      {
        hitEffect.GetComponentInChildren<ParticleSystem>()?.Play();
      }

      // 3. 触发伤害事件（扣血）
      DamageEventArgs damageEventArgs = new DamageEventArgs(
          element,
          gameObject,
          enemyView.gameObject,
          damage,
          isCritical,
          criticalMultiplier
      );

      // 使用事件通道触发伤害事件
      var damageChannel = EventChannelLocator.MainContainer?.damageEventChannel;
      damageChannel?.Raise(damageEventArgs);
      Debug.Log($"[CastNetwork] 通过事件通道触发伤害，目标ViewID: {enemyViewID}");
    }

    /// <summary>
    /// RPC：所有客户端显示伤害数字
    /// 用于在敌人被攻击时，显示伤害数字
    /// </summary>
    /// <param name="damageValue"></param>
    /// <param name="position"></param>
    [PunRPC]
    void RPC_ShowDamageNum(
            float damageValue,
            Vector3 position,
            bool isCritical,
            float criticalMultiplier
        )
    {
      if (isCritical)
      {
        damageValue *= criticalMultiplier;
      }
      damageValue = Mathf.Ceil(damageValue);
      Vector3 spawnPos = position + Vector3.up * 0f;
      // 1. 从对象池获取伤害数字对象
      GameObject damageNumObj = null;
      EventChannelLocator.MainContainer.poolOperationChannel.Raise(
          PoolOperationData.CreateGet(ObjectPoolConst.DamageNumPool, spawnPos, (o) => damageNumObj = o));
      if (damageNumObj != null)
      {
        DamageNum damageNum = damageNumObj.GetComponent<DamageNum>();
        if (damageNum != null)
        {
          damageNum.Play(damageValue, spawnPos, isCritical);
        }
      }
      else
      {
        Debug.LogError(
            $"DamageNumPool 为空，无法显示伤害数字：{damageValue}, {position}, {isCritical}"
        );
      }
    }

    // 在 CastNetwork.cs 中添加
    public void RequestSplitBullet(Vector3 spawnPos, Vector3 direction, int elementInt)
    {
      photonView.RPC(
          "RPC_OnSplitBulletCast",
          RpcTarget.Others,
          spawnPos,
          direction,
          elementInt
      );
    }

    [PunRPC]
    void RPC_OnSplitBulletCast(Vector3 spawnPos, Vector3 direction, int elementInt)
    {
      Element element = (Element)elementInt;
      SpawnSplitBulletForOthers(spawnPos, direction, element);
    }

    private void SpawnSplitBulletForOthers(Vector3 spawnPos, Vector3 direction, Element element)
    {
      // 为其他玩家生成分裂弹（只负责视觉表现）
      string visualPool = GetVisualPoolByElement(element);
      GameObject visualObj = null;
      EventChannelLocator.MainContainer.poolOperationChannel.Raise(
          PoolOperationData.CreateGet(visualPool, spawnPos, (o) => visualObj = o));

      if (visualObj != null)
      {
        visualObj.GetComponentInChildren<ParticleSystem>()?.Play();
        visualObj.transform.rotation = Quaternion.LookRotation(direction);
      }

      GameObject triggerObj = null;
      EventChannelLocator.MainContainer.poolOperationChannel.Raise(
          PoolOperationData.CreateGet(ObjectPoolConst.ImpactCannonTriggerPool, spawnPos, (o) => triggerObj = o));

      if (triggerObj != null)
      {
        ImpactCannon cannon = triggerObj.GetComponent<ImpactCannon>();
        if (cannon != null)
        {
          cannon.StartShoot(direction, isMine: false);
          cannon.canSplit = false;

          AttackToken token = new AttackToken
          {
            hitCollider = triggerObj,
            vfxEffect = visualObj,
            vfxPoolName = visualPool,
          };
        }
      }
    }
  }
}