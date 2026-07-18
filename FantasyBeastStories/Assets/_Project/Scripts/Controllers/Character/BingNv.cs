using System.Collections;
using System.Collections.Generic;
using NetworkTarget = Controllers.Network.NetworkTarget;
using Controllers.CardData;
using Core;
using Core.Channels.Player;
using Core;
using Controllers.Services;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Controllers.Character
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

        // ========== 对象池预制体（GuiLing 各元素版本） ==========
        [Header("GuiLing 预制体（各元素投射物）")]
        [SerializeField] private GameObject guiLingWinterPrefab;
        [SerializeField] private GameObject guiLingFirePrefab;
        [SerializeField] private GameObject guiLingLightningPrefab;
        [SerializeField] private GameObject guiLingGrassPrefab;

        [Header("GuiLing 击中特效预制体（各元素）")]
        [SerializeField] private GameObject guiLingWinterHitPrefab;
        [SerializeField] private GameObject guiLingFireHitPrefab;
        [SerializeField] private GameObject guiLingLightningHitPrefab;
        [SerializeField] private GameObject guiLingGrassHitPrefab;

        // ========== 多元素解锁系统（BingNv 可同时拥有多种元素） ==========

        /// <summary>
        /// 已解锁的元素集合（BingNv 不同于 WizardBoy 的单元素替换模式，
        /// 可同时解锁多种元素，AttackRange_BingNv 据此发射对应类型的 GuiLing）
        /// </summary>
        private readonly HashSet<Element> _unlockedElements = new HashSet<Element>();

        /// <summary>
        /// 暴露给外部（如 AttackRange_BingNv）读取已解锁的元素列表
        /// </summary>
        public IReadOnlyCollection<Element> UnlockedElements => _unlockedElements;

        // ========== Unity 生命周期 ==========

        protected override void Start()
        {
            base.Start();

            // ★ 仅本地玩家设置 MagicUpgradeManager 的角色卡牌类型
            // 防止非本地角色的 Start() 覆盖当前客户端的卡牌池
            if (NetworkServiceLocator.PlayerService.IsOwnerOf(gameObject))
            {
                // 通知 MagicUpgradeManager 当前角色为 BingNv，专属卡牌使用对应卡池
                MagicUpgradeManager.instance?.SetCurrentEventName(CharacterCardType.BingNv);

                // 默认解锁 Winter（冰女初始冰霜属性）
                UnlockElement(Element.Winter);
            }

            // TODO: 测试按钮绑定（参考 WizardBoy）
            // if (testCardEffect != null)
            // {
            //     testCardEffect.onClick.AddListener(() => { UnlockElement(Element.Fire); });
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
            // BingNv 不使用元素替换模式，改为解锁新元素
            // 但保留此方法以防外部调用，内部委托给 UnlockElement
            UnlockElement(element);
        }

        /// <summary>
        /// 解锁新元素（BingNv 可同时拥有多种元素，不同于 WizardBoy 的替换模式）
        /// 已解锁则跳过，未解锁则注册对象池 + 网络同步
        /// </summary>
        private void UnlockElement(Element element)
        {
            if (!_unlockedElements.Add(element))
                return; // 已解锁，跳过

            // 注册对应元素的对象池（投射物 + 击中特效）
            switch (element)
            {
                case Element.Winter:
                    EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                        PoolOperationData.CreateAddMultiple(PoolConst.GuiLingWinterPool, guiLingWinterPrefab, 10));
                    if (guiLingWinterHitPrefab != null)
                        EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                            PoolOperationData.CreateAddMultiple(PoolConst.GuiLingHitWinterPool, guiLingWinterHitPrefab, 20));
                    break;
                case Element.Fire:
                    EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                        PoolOperationData.CreateAddMultiple(PoolConst.GuiLingFirePool, guiLingFirePrefab, 10));
                    if (guiLingFireHitPrefab != null)
                        EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                            PoolOperationData.CreateAddMultiple(PoolConst.GuiLingHitFirePool, guiLingFireHitPrefab, 20));
                    break;
                case Element.Lightning:
                    EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                        PoolOperationData.CreateAddMultiple(PoolConst.GuiLingLightningPool, guiLingLightningPrefab, 10));
                    if (guiLingLightningHitPrefab != null)
                        EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                            PoolOperationData.CreateAddMultiple(PoolConst.GuiLingHitLightningPool, guiLingLightningHitPrefab, 20));
                    break;
                case Element.Grass:
                    EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                        PoolOperationData.CreateAddMultiple(PoolConst.GuiLingGrassPool, guiLingGrassPrefab, 10));
                    if (guiLingGrassHitPrefab != null)
                        EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                            PoolOperationData.CreateAddMultiple(PoolConst.GuiLingHitGrassPool, guiLingGrassHitPrefab, 20));
                    break;
            }

            // 网络同步（非测试模式）
            if (!EventChannelLocator.MainContainer.gameSettings.IsTest)
            {
                NetworkServiceLocator.DomainRpcService?.InvokeRPC(
                    "RPC_InitElementPool",
                    NetworkTarget.Others,
                    NetworkServiceLocator.ObjectService.GetViewID(gameObject),
                    (int)element
                );
            }
        }

        // ========== 卡牌效果 ==========

        protected override void OnApplicationCard(CardConfigBase card)
        {
            base.OnApplicationCard(card);

            // TODO: 冰女专属卡牌效果
            switch (card.Name)
            {
                case "凤羽流火":
                    attributePlayer.AddAttackPower(card.Value);
                    UnlockElement(Element.Fire);
                    break;
                case "木翎回春":
                    movementData.healthRecover += card.Value;
                    attributePlayer.SetHealthRecover(movementData.healthRecover);
                    UnlockElement(Element.Grass);
                    break;
                case "雷翎惊鸿":
                    UnlockElement(Element.Lightning);
                    attributePlayer.AddAttackPower(card.Value);
                    break;
                case "叠羽追风":
                    attributePlayer.AddComboCount(card.Value);
                    break;
                case "穿林打叶":
                    attributePlayer.AddMultiTargetCount(card.Value);
                    break;
                case "一羽千翎":
                    attributePlayer.AddSplitCount(card.Value);
                    attributePlayer.SetSplit(true);
                    break;
            }
        }

        // ========== 网络 RPC 回调 ==========

        /// <summary>
        /// 由 DomainRpcBridge.RPC_InitElementPool 调用 — 在其他客户端初始化元素对象池
        /// </summary>
        public void HandleInitElementPool(int elementInt)
        {
            Element element = (Element)elementInt;

            switch (element)
            {
                case Element.Winter:
                    EnsurePoolCreated(PoolConst.GuiLingWinterPool, guiLingWinterPrefab, 10);
                    if (guiLingWinterHitPrefab != null)
                        EnsurePoolCreated(PoolConst.GuiLingHitWinterPool, guiLingWinterHitPrefab, 20);
                    break;
                case Element.Fire:
                    EnsurePoolCreated(PoolConst.GuiLingFirePool, guiLingFirePrefab, 10);
                    if (guiLingFireHitPrefab != null)
                        EnsurePoolCreated(PoolConst.GuiLingHitFirePool, guiLingFireHitPrefab, 20);
                    break;
                case Element.Lightning:
                    EnsurePoolCreated(PoolConst.GuiLingLightningPool, guiLingLightningPrefab, 10);
                    if (guiLingLightningHitPrefab != null)
                        EnsurePoolCreated(PoolConst.GuiLingHitLightningPool, guiLingLightningHitPrefab, 20);
                    break;
                case Element.Grass:
                    EnsurePoolCreated(PoolConst.GuiLingGrassPool, guiLingGrassPrefab, 10);
                    if (guiLingGrassHitPrefab != null)
                        EnsurePoolCreated(PoolConst.GuiLingHitGrassPool, guiLingGrassHitPrefab, 20);
                    break;
            }
        }

        /// <summary>
        /// 检查池是否存在，不存在则创建（网络同步用）
        /// </summary>
        private void EnsurePoolCreated(string poolName, GameObject prefab, int count)
        {
            int currentCount = 0;
            EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                PoolOperationData.CreateGetPoolCount(poolName, (c) => currentCount = c));
            if (currentCount == 0)
            {
                EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                    PoolOperationData.CreateAddMultiple(poolName, prefab, count));
            }
        }

        // ========== 场景加载 ==========

        protected override void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // TODO: 冰女专属的场景加载逻辑（如有）
        }
    }
}