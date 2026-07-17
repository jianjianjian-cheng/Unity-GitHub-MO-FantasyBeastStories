using System.Collections;
using System.Collections.Generic;
using Controllers.CardData;
using Core;
using Core.Channels.Player;
using Core;
using Controllers.Services;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Controllers.Character
{
  public class WizardBoy : PlayerController
  {
    [SerializeField]
    private Button testCardEffect;

    [Header("对象池预制体")]
    [SerializeField] private GameObject impactCannonLightenPrefab;
    [SerializeField] private GameObject impactCannonHitLightenPrefab;
    [SerializeField] private GameObject impactCannonWinterPrefab;
    [SerializeField] private GameObject impactCannonHitWinterPrefab;
    [SerializeField] private GameObject impactCannonGrassPrefab;
    [SerializeField] private GameObject impactCannonHitGrassPrefab;

    protected override void Start()
    {
      base.Start();

      // ★ 仅本地玩家设置 MagicUpgradeManager 的角色卡牌类型
      // 防止非本地角色的 Start() 覆盖当前客户端的卡牌池
      if (NetworkServiceLocator.PlayerService.IsOwnerOf(gameObject))
      {
        // 通知 MagicUpgradeManager 当前角色为 WizardBoy，专属卡牌使用对应卡池
        MagicUpgradeManager.instance?.SetCurrentEventName(CharacterCardType.WizardBoy);

        if (testCardEffect != null)
        {
          testCardEffect.onClick.AddListener(() =>
          {
            SwitchElement(Element.Grass);
          });
        }
      }
    }

    protected override void OnEnable()
    {
      base.OnEnable();
    }

    protected override void OnDisable()
    {
      base.OnDisable();
    }

    protected override void OnSkillQuery(SkillQueryData data)
    {
      if (data.queryType == SkillQueryType.GetMaxAttackCount)
      {
        data.intValue = GetMaxAttackCount();
      }
    }

    protected override void SwitchElement(Element element)
    {
      switch (element)
      {
        case Element.Lightning:
          EventChannelLocator.MainContainer.poolOperationChannel.Raise(
              PoolOperationData.CreateAddMultiple(ObjectPoolConst.ImpactCannonLightenPool, impactCannonLightenPrefab, 10));
          EventChannelLocator.MainContainer.poolOperationChannel.Raise(
              PoolOperationData.CreateAddMultiple(ObjectPoolConst.ImpactCannonHitLightenPool, impactCannonHitLightenPrefab, 20));
          break;
        case Element.Winter:
          EventChannelLocator.MainContainer.poolOperationChannel.Raise(
              PoolOperationData.CreateAddMultiple(ObjectPoolConst.ImpactCannonWinterPool, impactCannonWinterPrefab, 10));
          EventChannelLocator.MainContainer.poolOperationChannel.Raise(
              PoolOperationData.CreateAddMultiple(ObjectPoolConst.ImpactCannonHitWinterPool, impactCannonHitWinterPrefab, 20));
          break;
        case Element.Grass:
          EventChannelLocator.MainContainer.poolOperationChannel.Raise(
              PoolOperationData.CreateAddMultiple(ObjectPoolConst.ImpactCannonGrassPool, impactCannonGrassPrefab, 10));
          EventChannelLocator.MainContainer.poolOperationChannel.Raise(
              PoolOperationData.CreateAddMultiple(ObjectPoolConst.ImpactCannonHitGrassPool, impactCannonHitGrassPrefab, 20));
          break;
      }

      if (!EventChannelLocator.MainContainer.gameSettings.IsTest)
      {
        NetworkServiceLocator.DomainRpcService?.InvokeRPC(
            "RPC_InitElementPool",
            NetworkTarget.Others,
            NetworkServiceLocator.ObjectService.GetViewID(gameObject),
            (int)element
        );
      }

      base.SwitchElement(element);
    }

    protected override void OnApplicationCard(CardConfigBase card)
    {
      base.OnApplicationCard(card);
      switch (card.Name)
      {
        case "流光穿透":
          attributePlayer.AddMaxAttackCount(1);
          break;
        case "流光连射":
          attributePlayer.AddComboCount(1);
          break;
        case "惊鸿照影":
          attributePlayer.SetCurrentElement(Element.Lightning);
          SwitchElement(Element.Lightning);
          attributePlayer.AddAttackPower(20);
          break;
        case "森芒初露":
          attributePlayer.SetCurrentElement(Element.Grass);
          SwitchElement(Element.Grass);
          movementData.healthRecover += 2;
          attributePlayer.SetHealthRecover(movementData.healthRecover);
          break;
        case "流光分裂":
          attributePlayer.SetSplit(true);
          attributePlayer.AddSplitCount(1);
          break;
        case "碎雪回风":
          attributePlayer.SetCurrentElement(Element.Winter);
          SwitchElement(Element.Winter);
          attributePlayer.AddAttackPower(20);
          break;
      }
    }

    /// <summary>
    /// 由 DomainRpcBridge.RPC_InitElementPool 调用 — 在其他客户端初始化元素对象池
    /// </summary>
    public void HandleInitElementPool(int elementInt)
    {
      Element element = (Element)elementInt;

      switch (element)
      {
        case Element.Lightning:
          int countLighten = 0;
          EventChannelLocator.MainContainer.poolOperationChannel.Raise(
              PoolOperationData.CreateGetPoolCount(ObjectPoolConst.ImpactCannonLightenPool, (c) => countLighten = c));
          if (countLighten == 0)
          {
            EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                PoolOperationData.CreateAddMultiple(ObjectPoolConst.ImpactCannonLightenPool, impactCannonLightenPrefab, 10));
          }
          int countHitLighten = 0;
          EventChannelLocator.MainContainer.poolOperationChannel.Raise(
              PoolOperationData.CreateGetPoolCount(ObjectPoolConst.ImpactCannonHitLightenPool, (c) => countHitLighten = c));
          if (countHitLighten == 0)
          {
            EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                PoolOperationData.CreateAddMultiple(ObjectPoolConst.ImpactCannonHitLightenPool, impactCannonHitLightenPrefab, 20));
          }
          break;
        case Element.Winter:
          int countWinter = 0;
          EventChannelLocator.MainContainer.poolOperationChannel.Raise(
              PoolOperationData.CreateGetPoolCount(ObjectPoolConst.ImpactCannonWinterPool, (c) => countWinter = c));
          if (countWinter == 0)
          {
            EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                PoolOperationData.CreateAddMultiple(ObjectPoolConst.ImpactCannonWinterPool, impactCannonWinterPrefab, 10));
          }
          int countHitWinter = 0;
          EventChannelLocator.MainContainer.poolOperationChannel.Raise(
              PoolOperationData.CreateGetPoolCount(ObjectPoolConst.ImpactCannonHitWinterPool, (c) => countHitWinter = c));
          if (countHitWinter == 0)
          {
            EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                PoolOperationData.CreateAddMultiple(ObjectPoolConst.ImpactCannonHitWinterPool, impactCannonHitWinterPrefab, 20));
          }
          break;
        case Element.Grass:
          int countGrass = 0;
          EventChannelLocator.MainContainer.poolOperationChannel.Raise(
              PoolOperationData.CreateGetPoolCount(ObjectPoolConst.ImpactCannonGrassPool, (c) => countGrass = c));
          if (countGrass == 0)
          {
            EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                PoolOperationData.CreateAddMultiple(ObjectPoolConst.ImpactCannonGrassPool, impactCannonGrassPrefab, 10));
          }
          int countHitGrass = 0;
          EventChannelLocator.MainContainer.poolOperationChannel.Raise(
              PoolOperationData.CreateGetPoolCount(ObjectPoolConst.ImpactCannonHitGrassPool, (c) => countHitGrass = c));
          if (countHitGrass == 0)
          {
            EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                PoolOperationData.CreateAddMultiple(ObjectPoolConst.ImpactCannonHitGrassPool, impactCannonHitGrassPrefab, 20));
          }
          break;
      }
    }
  }
}