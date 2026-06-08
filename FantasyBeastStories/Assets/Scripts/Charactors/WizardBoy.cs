using System.Collections;
using System.Collections.Generic;
using CardData;
using Charactors;
using Manager;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WizardBoy : PlayerController
{
    [SerializeField]
    private Button testCardEffect;

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
        EventManager.instance.RegisterCardEvent(
            EventNames.OnReceiveCard_WizardBoy,
            OnApplicationCard
        );
        EventManager.instance.RegisterIntReturnEvent(
            EventNames.OnGetMaxAttackCount_WizardBoy,
            GetMaxAttackCount
        );
        SceneManager.sceneLoaded += OnSceneLoad;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EventManager.instance.UnRegisterCardEvent(EventNames.OnReceiveCard_WizardBoy);
        SceneManager.sceneLoaded -= OnSceneLoad;
    }

    protected override void SwitchElement(Element element)
    {
        switch (element)
        {
            case Element.Lightning:
                ObjectPoolManager.instance.AddMultipleToPool(
                    ObjectPoolConst.ImpactCannonLightenPool,
                    ObjectPoolManager.instance.ImpactCannonLightenPrefab,
                    10
                );
                ObjectPoolManager.instance.AddMultipleToPool(
                    ObjectPoolConst.ImpactCannonHitLightenPool,
                    ObjectPoolManager.instance.IImpactCannonHitLightenPrefab,
                    20
                );
                break;
            case Element.Winter:
                ObjectPoolManager.instance.AddMultipleToPool(
                    ObjectPoolConst.ImpactCannonWinterPool,
                    ObjectPoolManager.instance.ImpactCannonWinterPrefab,
                    10
                );
                ObjectPoolManager.instance.AddMultipleToPool(
                    ObjectPoolConst.ImpactCannonHitWinterPool,
                    ObjectPoolManager.instance.ImpactCannonHitWinterPrefab,
                    20
                );
                break;
            case Element.Grass:
                ObjectPoolManager.instance.AddMultipleToPool(
                    ObjectPoolConst.ImpactCannonGrassPool,
                    ObjectPoolManager.instance.ImpactCannonGrassPrefab,
                    10
                );
                ObjectPoolManager.instance.AddMultipleToPool(
                    ObjectPoolConst.ImpactCannonHitGrassPool,
                    ObjectPoolManager.instance.ImpactCannonHitGrassPrefab,
                    20
                );
                break;
        }

        base.SwitchElement(element);
    }

    protected override void OnApplicationCard(CardConfigBase card)
    {
        base.OnApplicationCard(card);
        //------小法师专属卡牌强化------
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
            case "流光治愈":
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

    private void OnSceneLoad(Scene scene, LoadSceneMode mode) { }
}
