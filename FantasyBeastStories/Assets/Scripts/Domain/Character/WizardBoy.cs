using System.Collections;
using System.Collections.Generic;
using Domain.CardData;
using Domain.Event;
using Domain.Event.Channels.Player;
using Domain.Pool;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Domain.Character
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
      if (testCardEffect != null)
      {
        testCardEffect.onClick.AddListener(() =>
        {
          SwitchElement(Element.Grass);
        });
      }
    }

    protected override void OnEnable()
    {
      base.OnEnable();
      EventChannelLocator.MainContainer.cardReceivedChannel.RegisterListener(OnApplicationCard);
      EventChannelLocator.MainContainer.skillQueryChannel.RegisterListener(OnSkillQuery);
      SceneManager.sceneLoaded += OnSceneLoad;
    }

    protected override void OnDisable()
    {
      base.OnDisable();
      EventChannelLocator.MainContainer.cardReceivedChannel.UnregisterListener(OnApplicationCard);
      EventChannelLocator.MainContainer.skillQueryChannel.UnregisterListener(OnSkillQuery);
      SceneManager.sceneLoaded -= OnSceneLoad;
    }

    private void OnSkillQuery(SkillQueryData data)
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

      if (!EventChannelLocator.MainContainer.gameSettings.IsTest && photonView != null)
      {
        photonView.RPC("RPC_InitElementPool", RpcTarget.Others, (int)element);
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
          healthRecover += 2;
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

    private int GetMaxAttackCount()
    {
      return attributePlayer.GetMaxAttackCount();
    }

    [PunRPC]
    public void RPC_InitElementPool(int elementInt)
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

    private void OnSceneLoad(Scene scene, LoadSceneMode mode) { }
  }
}