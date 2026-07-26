using Controllers.CardData;

namespace Core.SharedModel
{
    /// <summary>
    /// 魔法升级模型 — 纯 C# 类，不继承 MonoBehaviour，可独立单元测试。
    ///
    /// 持有：
    /// - 面板状态 (IsPanelActive / IsConfirmed)
    /// - 当前卡牌数据 (CardData[])
    /// - 专属卡模式标记 (IsAllExCard)
    /// - 当前角色事件名 (CurrentEventName)
    ///
    /// 卡牌选择编排逻辑：
    /// - 专属卡抽取判定（基于幸运值）
    /// - 卡牌数据获取委托（通过回调让 Controller 查询 MagicUpgradeInfoManager）
    ///
    /// 外部依赖（DOTween / ParticleSystem / NetworkServiceLocator / EventChannelSO）
    /// 由 Controller/View 处理，Model 只管理数据与判定逻辑。
    /// </summary>
    public class MagicUpgradeModel
    {
        // ──────────────────────────────────
        //  状态
        // ──────────────────────────────────

        public bool IsPanelActive { get; private set; }
        public bool IsConfirmed { get; private set; }
        public bool IsAllExCard { get; set; }
        public string CurrentEventName { get; private set; }

        // ──────────────────────────────────
        //  卡牌数据
        // ──────────────────────────────────

        public CardConfigSO[] CardData { get; private set; }

        // ──────────────────────────────────
        //  状态控制
        // ──────────────────────────────────

        public void Open()
        {
            IsPanelActive = true;
            IsConfirmed = false;
        }

        public void Close()
        {
            IsPanelActive = false;
            IsConfirmed = false;
        }

        public void Confirm()
        {
            IsConfirmed = true;
        }

        public void SetCurrentEventName(string eventName)
        {
            CurrentEventName = eventName;
        }

        public void SetCardData(CardConfigSO[] cards)
        {
            CardData = cards;
        }

        // ──────────────────────────────────
        //  卡牌选择编排
        // ──────────────────────────────────

        /// <summary>
        /// 获取三张卡牌（普通或专属），并处理幸运值专属卡抽取。
        ///
        /// 参数说明：
        /// - getCards: 由 Controller 提供的卡牌获取委托（查询 MagicUpgradeInfoManager）
        /// - getExCards: 专属卡获取委托
        /// - getRandomExCard: 单张专属卡获取委托
        /// - luckRate: 当前幸运值
        /// </summary>
        public CardConfigSO[] ResolveCardSelection(
            System.Func<CardConfigSO[]> getCards,
            System.Func<string, CardConfigSO> getRandomExCard,
            int luckRate)
        {
            CardConfigSO[] cards = getCards();
            if (cards == null) return null;

            // 专属卡模式：直接返回三张专属卡
            if (IsAllExCard)
            {
                IsAllExCard = false; // 消费标记
                return cards; // getCards 已返回专属卡集
            }

            // 幸运值判定：是否替换一张为专属卡
            float exCardChance = luckRate * 0.8f;
            if (UnityEngine.Random.Range(0f, 100f) < exCardChance)
            {
                CardConfigSO exCard = getRandomExCard(CurrentEventName);
                if (exCard != null && cards.Length > 0)
                {
                    int replaceIndex = UnityEngine.Random.Range(0, cards.Length);
                    cards[replaceIndex] = exCard;
                }
            }

            return cards;
        }

        /// <summary>
        /// 处理卡牌选择完成。
        /// 返回 true 表示本次确认有效（首次确认），false 表示重复确认。
        /// </summary>
        public bool TryConfirmSelection()
        {
            if (IsConfirmed)
                return false;

            IsConfirmed = true;
            return true;
        }
    }
}
