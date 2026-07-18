using System.Collections;
using System.Collections.Generic;
using Core;
using Controllers.Combat;
using Controllers.Combat.ImpactCannon;
using Photon.Pun;
using UnityEngine;
using Controllers.Network;
using UI;

namespace Controllers.Network
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
      GameObject visualObj = PoolHelper.Get(visualPool, spawnPos);
      if (visualObj != null)
      {
        visualObj.GetComponentInChildren<ParticleSystem>()?.Play();
        visualObj.transform.rotation = Quaternion.LookRotation(direction);
      }

      GameObject triggerObj = PoolHelper.Get(ObjectPoolConst.ImpactCannonTriggerPool, spawnPos);
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

      // 发给所有玩家：每个客户端通过事件通道分发伤害，EnemyBase.OnDamageReceived 校验 damgeTarget==gameObject 去重
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

      // 2. 触发伤害事件（扣血）
      // 注意：击中特效由各投射物系统自行处理，不在 RPC 中重复播放。
      // - ImpactCannon: ImpactCannon.OnTriggerEnter → PlayHitEffect()
      // - GuiLing:      GuiLingBase.OnTriggerEnter → LaunchEffect(hitVFX)
      Element element = (Element)elementInt;
      DamageEventArgs damageEventArgs = DamageEventArgs.GetShared(
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
      GameObject damageNumObj = PoolHelper.Get(ObjectPoolConst.DamageNumPool, spawnPos);
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

    // ==================== GuiLing（鬼灵弹）投射物同步 ====================

    /// <summary>
    /// 接口实现：请求广播 GuiLing 发射（由 AttackRange_BingNv 调用）
    /// </summary>
    public void RequestGuiLingCast(Vector3 spawnPos, Vector3 direction, int targetViewID, int elementInt)
    {
      photonView.RPC(
          "RPC_OnGuiLingCast",
          RpcTarget.Others,
          spawnPos,
          direction,
          targetViewID,
          elementInt
      );
    }

    /// <summary>
    /// RPC：其他客户端收到后，在本地生成 GuiLing 视觉投射物
    /// </summary>
    [PunRPC]
    void RPC_OnGuiLingCast(Vector3 spawnPos, Vector3 direction, int targetViewID, int elementInt)
    {
      Element element = (Element)elementInt;
      SpawnGuiLingForOthers(spawnPos, direction, targetViewID, element);
    }

    /// <summary>
    /// 为其他客户端生成本地 GuiLing（isMine = false，仅视觉表现）
    /// </summary>
    private void SpawnGuiLingForOthers(Vector3 spawnPos, Vector3 direction, int targetViewID, Element element)
    {
      string poolName = GetGuiLingPoolByElement(element);

      GameObject guiLing = PoolHelper.Get(poolName, spawnPos);
      if (guiLing == null)
      {
        Debug.LogWarning($"[CastNetwork] 从对象池 {poolName} 获取 GuiLing 失败");
        return;
      }

      // 查找目标
      PhotonView targetView = PhotonView.Find(targetViewID);
      Transform target = targetView != null ? targetView.transform : null;

      var guiLingBase = guiLing.GetComponent<GuiLingBase>();
      guiLingBase.poolName = poolName;
      guiLingBase.SetTargetAndLaunch(target, direction);
      // 重要：isMine = false，此投射物不负责伤害判定
      guiLingBase.SetDamageData(false, 0f, 0f, 1f, element);
      guiLingBase.SetSplitData(false, 0);
    }

    /// <summary>
    /// 根据元素类型获取 GuiLing 对象池名称
    /// </summary>
    private static string GetGuiLingPoolByElement(Element element)
    {
      switch (element)
      {
        case Element.Fire: return PoolConst.GuiLingFirePool;
        case Element.Lightning: return PoolConst.GuiLingLightningPool;
        case Element.Grass: return PoolConst.GuiLingGrassPool;
        case Element.Winter:
        default: return PoolConst.GuiLingWinterPool;
      }
    }

    // ==================== GuiLing 分裂弹同步 ====================

    /// <summary>
    /// 广播 GuiLing 分裂弹（由 GuiLingBase 命中时调用）
    /// 其他客户端收到后生成本地视觉分裂弹
    /// </summary>
    public void BroadcastSplitGuiLingCast(Vector3 spawnPos, Vector3 direction, GameObject targetEnemy, int elementInt)
    {
      PhotonView targetView = targetEnemy.GetComponent<PhotonView>();
      if (targetView == null)
        return;

      if (photonView == null)
        return;

      photonView.RPC(
          "RPC_OnSplitGuiLingCast",
          RpcTarget.Others,
          spawnPos,
          direction,
          targetView.ViewID,
          elementInt
      );
    }

    /// <summary>
    /// RPC：其他客户端收到后，在本地生成 GuiLing 分裂弹
    /// </summary>
    [PunRPC]
    void RPC_OnSplitGuiLingCast(Vector3 spawnPos, Vector3 direction, int targetViewID, int elementInt)
    {
      Element element = (Element)elementInt;
      SpawnGuiLingForOthers(spawnPos, direction, targetViewID, element);
    }

    // ==================== GuiLing 击中特效同步 ====================

    /// <summary>
    /// 广播 GuiLing 击中特效（由 GuiLingBase 命中时调用）
    /// 其他客户端在精确位置播放击中特效，确保所有玩家看到一致的命中效果
    /// </summary>
    public void BroadcastGuiLingHitVFX(Vector3 hitPos, Vector3 normal, int elementInt)
    {
      if (photonView == null)
        return;

      photonView.RPC(
          "RPC_PlayGuiLingHitVFX",
          RpcTarget.Others,
          hitPos,
          normal,
          elementInt
      );
    }

    /// <summary>
    /// RPC：其他客户端收到后，在本地播放 GuiLing 击中特效
    /// </summary>
    [PunRPC]
    void RPC_PlayGuiLingHitVFX(Vector3 hitPos, Vector3 normal, int elementInt)
    {
      Element element = (Element)elementInt;
      string hitPoolName = GetGuiLingHitPoolByElement(element);
      if (string.IsNullOrEmpty(hitPoolName))
        return;

      GameObject hitEffect = PoolHelper.Get(hitPoolName, hitPos);
      if (hitEffect == null)
        return;

      hitEffect.transform.position = hitPos;
      hitEffect.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
      // GuiLingHit 脚本会在 OnEnable 中自动播放粒子并归还池
    }

    /// <summary>
    /// 根据元素类型获取 GuiLing 击中特效对象池名称
    /// </summary>
    private static string GetGuiLingHitPoolByElement(Element element)
    {
      switch (element)
      {
        case Element.Fire: return PoolConst.GuiLingHitFirePool;
        case Element.Lightning: return PoolConst.GuiLingHitLightningPool;
        case Element.Grass: return PoolConst.GuiLingHitGrassPool;
        case Element.Winter:
        default: return PoolConst.GuiLingHitWinterPool;
      }
    }

    // ==================== ImpactCannon 分裂弹（已有） ====================

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
      GameObject visualObj = PoolHelper.Get(visualPool, spawnPos);

      if (visualObj != null)
      {
        visualObj.GetComponentInChildren<ParticleSystem>()?.Play();
        visualObj.transform.rotation = Quaternion.LookRotation(direction);
      }

      GameObject triggerObj = PoolHelper.Get(ObjectPoolConst.ImpactCannonTriggerPool, spawnPos);

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