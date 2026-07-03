using System.Collections;
using System.Collections.Generic;
using Domain.CardData;
using Domain.Event;
using Domain.Event.Channels.Player;
using Domain.Pool;
using Domain.Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Domain.Character
{
    /// <summary>
    /// 冰女（BingNv）—— 冰霜系角色
    ///
    /// 设计说明：
    /// - 继承 PlayerController，父类已处理通用频道注册（cardReceivedChannel / skillQueryChannel / sceneLoaded）
    /// - 子类只需重写需要个性化处理的方法
    /// - 实际逻辑部分当前为空，请根据角色设计填充
    /// </summary>
    public class BingNv : PlayerController
    {
        // ========== 测试 ==========
        [SerializeField]
        private Button testCardEffect;

        // ========== 对象池预制体（按角色实际需要的元素配置） ==========
        [Header("对象池预制体")]
        [SerializeField] private GameObject impactCannonWinterPrefab;
        [SerializeField] private GameObject impactCannonHitWinterPrefab;

        // ========== Unity 生命周期 ==========

        protected override void Start()
        {
            base.Start();
            // TODO: 测试按钮绑定（参考 WizardBoy）
            // if (testCardEffect != null)
            // {
            //     testCardEffect.onClick.AddListener(() => { SwitchElement(Element.Winter); });
            // }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // 父类已注册 cardReceivedChannel / skillQueryChannel / sceneLoaded
            // 如有额外频道需要在此注册
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            // 父类已注销通用频道
            // 如有额外频道需要在此注销
        }

        // ========== 技能查询回调 ==========

        protected override void OnSkillQuery(SkillQueryData data)
        {
            // TODO: 处理冰女专属的技能查询
            // 例：
            // if (data.queryType == SkillQueryType.GetMaxAttackCount)
            // {
            //     data.intValue = GetMaxAttackCount();
            // }
        }

        // ========== 元素切换 ==========

        protected override void SwitchElement(Element element)
        {
            // TODO: 初始化冰女专属的对象池
            // switch (element)
            // {
            //     case Element.Winter:
            //         EventChannelLocator.MainContainer.poolOperationChannel.Raise(
            //             PoolOperationData.CreateAddMultiple(ObjectPoolConst.ImpactCannonWinterPool, impactCannonWinterPrefab, 10));
            //         EventChannelLocator.MainContainer.poolOperationChannel.Raise(
            //             PoolOperationData.CreateAddMultiple(ObjectPoolConst.ImpactCannonHitWinterPool, impactCannonHitWinterPrefab, 20));
            //         break;
            // }

            // 网络同步（非测试模式）
            // if (!EventChannelLocator.MainContainer.gameSettings.IsTest)
            // {
            //     NetworkServiceLocator.DomainRpcService?.InvokeRPC(
            //         "RPC_InitElementPool",
            //         NetworkTarget.Others,
            //         NetworkServiceLocator.ObjectService.GetViewID(gameObject),
            //         (int)element
            //     );
            // }

            base.SwitchElement(element);
        }

        // ========== 卡牌效果 ==========

        protected override void OnApplicationCard(CardConfigBase card)
        {
            base.OnApplicationCard(card);

            // TODO: 冰女专属卡牌效果
            // switch (card.Name)
            // {
            //     case "冰霜新星":
            //         attributePlayer.AddAttackPower(10);
            //         break;
            //     case "寒冰护盾":
            //         attributePlayer.AddDefensePower(15);
            //         break;
            // }
        }

        // ========== 网络 RPC 回调 ==========

        /// <summary>
        /// 由 DomainRpcBridge.RPC_InitElementPool 调用 — 在其他客户端初始化元素对象池
        /// </summary>
        public void HandleInitElementPool(int elementInt)
        {
            Element element = (Element)elementInt;

            // TODO: 在其他客户端初始化元素对象池
            // switch (element)
            // {
            //     case Element.Winter:
            //         // 检查池数量，为 0 则创建
            //         break;
            // }
        }

        // ========== 场景加载 ==========

        protected override void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // TODO: 冰女专属的场景加载逻辑（如有）
        }
    }
}