using System.Collections;
using System.Collections.Generic;
using CardData;
using Charactors;
using Manager;
using Photon.Pun;
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
        // 本地初始化对象池
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

        // 发送 RPC 通知其他客户端初始化对象池
        if (!GameManager.isTest && photonView != null)
        {
            photonView.RPC("RPC_InitElementPool", RpcTarget.Others, (int)element);
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

    // 在 SwitchElement 方法后添加 RPC 方法
    [PunRPC]
    public void RPC_InitElementPool(int elementInt)
    {
        Element element = (Element)elementInt;

        switch (element)
        {
            case Element.Lightning:
                // 检查池是否已存在且有对象
                if (
                    ObjectPoolManager.instance.GetPoolCount(ObjectPoolConst.ImpactCannonLightenPool)
                    == 0
                )
                {
                    ObjectPoolManager.instance.AddMultipleToPool(
                        ObjectPoolConst.ImpactCannonLightenPool,
                        ObjectPoolManager.instance.ImpactCannonLightenPrefab,
                        10
                    );
                }
                if (
                    ObjectPoolManager.instance.GetPoolCount(
                        ObjectPoolConst.ImpactCannonHitLightenPool
                    ) == 0
                )
                {
                    ObjectPoolManager.instance.AddMultipleToPool(
                        ObjectPoolConst.ImpactCannonHitLightenPool,
                        ObjectPoolManager.instance.IImpactCannonHitLightenPrefab,
                        20
                    );
                }
                break;
            case Element.Winter:
                if (
                    ObjectPoolManager.instance.GetPoolCount(ObjectPoolConst.ImpactCannonWinterPool)
                    == 0
                )
                {
                    ObjectPoolManager.instance.AddMultipleToPool(
                        ObjectPoolConst.ImpactCannonWinterPool,
                        ObjectPoolManager.instance.ImpactCannonWinterPrefab,
                        10
                    );
                }
                if (
                    ObjectPoolManager.instance.GetPoolCount(
                        ObjectPoolConst.ImpactCannonHitWinterPool
                    ) == 0
                )
                {
                    ObjectPoolManager.instance.AddMultipleToPool(
                        ObjectPoolConst.ImpactCannonHitWinterPool,
                        ObjectPoolManager.instance.ImpactCannonHitWinterPrefab,
                        20
                    );
                }
                break;
            case Element.Grass:
                if (
                    ObjectPoolManager.instance.GetPoolCount(ObjectPoolConst.ImpactCannonGrassPool)
                    == 0
                )
                {
                    ObjectPoolManager.instance.AddMultipleToPool(
                        ObjectPoolConst.ImpactCannonGrassPool,
                        ObjectPoolManager.instance.ImpactCannonGrassPrefab,
                        10
                    );
                }
                if (
                    ObjectPoolManager.instance.GetPoolCount(
                        ObjectPoolConst.ImpactCannonHitGrassPool
                    ) == 0
                )
                {
                    ObjectPoolManager.instance.AddMultipleToPool(
                        ObjectPoolConst.ImpactCannonHitGrassPool,
                        ObjectPoolManager.instance.ImpactCannonHitGrassPrefab,
                        20
                    );
                }
                break;
        }
    }

    private void OnSceneLoad(Scene scene, LoadSceneMode mode) { }
}
