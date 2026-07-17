using System.Collections.Generic;
using Controllers.CardData;
using Core;
using Core.Channels.Player;
using UnityEngine;

namespace UI
{
    public class MagicUpgradeInfoManager : MonoBehaviour
    {
        [SerializeField] private string eventNameToUse;

        void Awake()
        {
            LocalCard();
        }

        void OnEnable()
        {
            EventChannelLocator.MainContainer.skillQueryChannel.RegisterListener(OnSkillQuery);
        }

        void OnDisable()
        {
            EventChannelLocator.MainContainer.skillQueryChannel.UnregisterListener(OnSkillQuery);
        }

        private void OnSkillQuery(SkillQueryData data)
        {
            switch (data.queryType)
            {
                case SkillQueryType.GetLuckRate:
                    data.intValue = luckRate;
                    break;
                case SkillQueryType.AddLuckRate:
                    luckRate += data.intValue;
                    break;
                case SkillQueryType.GetRandomEXCard:
                    data.cardResult = GetRandomEXCard(data.cardType);
                    break;
                case SkillQueryType.GetThreeRandomEXCards:
                    data.cardsResult = GetThreeRandomEXCards(data.cardType);
                    break;
                case SkillQueryType.GetThreeRandomCards:
                    data.cardsResult = GetThreeRandomCards();
                    break;
            }
        }

        public CardDatabasePublic cardDatabasePublic;
        public CardDatabaseEX cardDatabaseEX;
        public CardConfigPublicNormal[] cardsPublicNormal;
        public CardConfigPublicEpic[] cardsPublicEpic;
        public CardConfigPublicLegend[] cardsPublicLegend;
        private int luckRate = 10;

        public void AddLuckRate(int luckRate)
        {
            this.luckRate += luckRate;
        }

        public int GetLuckRate()
        {
            return this.luckRate;
        }

        private void LocalCard()
        {
            if (cardDatabasePublic == null)
            {
                Debug.LogWarning($"MagicUpgradeInfoManager: cardDatabasePublic is not assigned in Inspector on {gameObject.name}");
                return;
            }
            cardsPublicNormal = cardDatabasePublic.cardsPublicNormal;
            cardsPublicEpic = cardDatabasePublic.cardsPublicEpic;
            cardsPublicLegend = cardDatabasePublic.cardsPublicLegend;
        }

        /// <summary>
        /// 获取三张不重复的卡牌，不固定品质位置
        /// 返回基类数组，每个元素可能是普通/史诗/传说任意一种
        /// </summary>
        public CardConfigBase[] GetThreeRandomCards()
        {
            CardConfigBase[] result = new CardConfigBase[3];
            HashSet<string> selectedNames = new HashSet<string>();

            for (int i = 0; i < 3; i++)
            {
                string rolledQuality = RollQualityByLuck();
                CardConfigBase card = GetRandomCardByQuality(rolledQuality, selectedNames);

                // 该品质没找到，从所有剩余中补一张
                if (card == null)
                    card = GetAnyAvailableCard(selectedNames);

                if (card != null)
                {
                    result[i] = card;
                    selectedNames.Add(card.Name);
                }
            }

            return result;
        }

        /// <summary>
        /// 根据品质获取随机卡牌
        /// </summary>
        private CardConfigBase GetRandomCardByQuality(string quality, HashSet<string> excludeNames)
        {
            switch (quality)
            {
                case "普通":
                    return GetOneFromPool(cardsPublicNormal, excludeNames);
                case "史诗":
                    return GetOneFromPool(cardsPublicEpic, excludeNames);
                case "传说":
                    return GetOneFromPool(cardsPublicLegend, excludeNames);
            }
            return null;
        }

        /// <summary>
        /// 从指定池子随机取一张未被选中的（泛型通用方法）
        /// </summary>
        private T GetOneFromPool<T>(T[] pool, HashSet<string> excludeNames)
            where T : CardConfigBase
        {
            if (pool == null || pool.Length == 0)
                return null;

            List<T> available = new List<T>();
            foreach (var card in pool)
            {
                if (!excludeNames.Contains(card.Name))
                    available.Add(card);
            }

            if (available.Count == 0)
                return null;
            return available[Random.Range(0, available.Count)];
        }

        /// <summary>
        /// 兜底：从所有剩余卡牌中随便取一张
        /// </summary>
        private CardConfigBase GetAnyAvailableCard(HashSet<string> excludeNames)
        {
            List<CardConfigBase> all = new List<CardConfigBase>();
            AddPoolToList(all, cardsPublicNormal, excludeNames);
            AddPoolToList(all, cardsPublicEpic, excludeNames);
            AddPoolToList(all, cardsPublicLegend, excludeNames);

            if (all.Count == 0)
                return null;
            return all[Random.Range(0, all.Count)];
        }

        private void AddPoolToList<T>(
            List<CardConfigBase> list,
            T[] pool,
            HashSet<string> excludeNames
        )
            where T : CardConfigBase
        {
            if (pool == null)
                return;
            foreach (var card in pool)
            {
                if (!excludeNames.Contains(card.Name))
                    list.Add(card);
            }
        }

        /// <summary>
        /// 摇品质
        /// </summary>
        private string RollQualityByLuck()
        {
            float normalChance = 70f;
            float epicChance = 20f;
            float legendChance = 10f;

            float luckBonus = luckRate * 0.5f;
            legendChance += luckBonus;
            epicChance += luckRate * 0.3f;
            normalChance -= luckRate * 0.8f;

            normalChance = Mathf.Max(normalChance, 10f);
            epicChance = Mathf.Max(epicChance, 10f);
            legendChance = Mathf.Max(legendChance, 5f);

            float total = normalChance + epicChance + legendChance;
            float roll = Random.Range(0f, total);

            if (roll < normalChance)
                return "普通";
            else if (roll < normalChance + epicChance)
                return "史诗";
            else
                return "传说";
        }

        //-------以下是专属卡牌效果抽取---------
        // 在类的顶部添加一个私有字段来记录已选中的卡牌
        private HashSet<string> selectedEXCardNames = new HashSet<string>();

        /// <summary>
        /// 从 CardDatabaseEX 中根据传入的类型参数随机抽取一张卡牌（等概率）
        /// 会自动记录已选卡牌，根据 Stackable 属性判断是否可重复选取
        /// </summary>
        /// <param name="cardType">卡牌类型：0-WizardBoy, 1-预留, 2-预留</param>
        /// <returns>随机抽取的卡牌</returns>
        public CardConfigBase GetRandomEXCard(string cardType)
        {
            if (cardDatabaseEX == null)
            {
                Debug.LogWarning("CardDatabaseEX 未赋值！");
                return null;
            }

            CardConfigBase card = null;

            switch (cardType)
            {
                case CharacterCardType.WizardBoy:
                    card = GetRandomFromArrayWithStackable(cardDatabaseEX.cardsEX_WizardBoy);
                    break;
                case CharacterCardType.BingNv:
                    card = GetRandomFromArrayWithStackable(cardDatabaseEX.cardsEX_BingNv);
                    break;
                case "待定2":
                    // TODO: 后续扩展
                    Debug.LogWarning("CardType 2 尚未实现");
                    return null;
                default:
                    Debug.LogWarning($"无效的 CardType: {cardType}");
                    return null;
            }

            // 抽到卡牌后自动记录
            if (card != null)
            {
                selectedEXCardNames.Add(card.Name);
            }

            return card;
        }

        /// <summary>
        /// 从指定数组中随机取一张卡牌（支持 Stackable 逻辑）
        /// Stackable为true的卡牌可以被重复选取，Stackable为false的卡牌如果已被选中则排除
        /// </summary>
        private CardConfigBase GetRandomFromArrayWithStackable(CardConfigEX[] array)
        {
            if (array == null || array.Length == 0)
            {
                Debug.LogWarning("卡牌数组为空！");
                return null;
            }

            // 构建可用列表
            List<CardConfigEX> available = new List<CardConfigEX>();

            foreach (var card in array)
            {
                // 如果该卡牌不可堆叠且已被选过，则跳过
                if (!card.Stackable && selectedEXCardNames.Contains(card.Name))
                    continue;

                available.Add(card);
            }

            if (available.Count == 0)
            {
                Debug.LogWarning("没有可用的卡牌！");
                return null;
            }

            return available[Random.Range(0, available.Count)];
        }

        /// <summary>
        /// 获取三张不重复的专属卡牌
        /// 会自动根据 Stackable 属性判断是否可重复选取
        /// </summary>
        /// <param name="cardType">卡牌类型：0-WizardBoy, 1-预留, 2-预留</param>
        /// <returns>三张专属卡牌数组，可能少于三张（可用卡牌不足时）</returns>
        public CardConfigBase[] GetThreeRandomEXCards(string cardType)
        {
            CardConfigBase[] result = new CardConfigBase[3];
            HashSet<string> currentSelection = new HashSet<string>();

            for (int i = 0; i < 3; i++)
            {
                CardConfigBase card = GetRandomEXCardInternal(cardType, currentSelection);

                if (card != null)
                {
                    result[i] = card;
                    currentSelection.Add(card.Name);
                    // 同时记录到全局已选集合中
                    selectedEXCardNames.Add(card.Name);
                }
                else
                {
                    // 可用卡牌不足，提前结束
                    Debug.LogWarning($"只能抽到 {i} 张卡牌，可用卡牌不足");
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// 内部抽取方法，支持传入额外的排除列表（当前三选中的排除）
        /// 这样可以在三选过程中保证不重复
        /// </summary>
        private CardConfigBase GetRandomEXCardInternal(
            string cardType,
            HashSet<string> extraExcludeNames
        )
        {
            if (cardDatabaseEX == null)
            {
                Debug.LogWarning("CardDatabaseEX 未赋值！");
                return null;
            }

            CardConfigEX[] targetArray = null;

            switch (cardType)
            {
                case CharacterCardType.WizardBoy:
                    targetArray = cardDatabaseEX.cardsEX_WizardBoy;
                    break;
                case CharacterCardType.BingNv:
                    targetArray = cardDatabaseEX.cardsEX_BingNv;
                    break;
                case "待定2":
                    Debug.LogWarning("CardType 2 尚未实现");
                    return null;
                default:
                    Debug.LogWarning($"无效的 CardType: {cardType}");
                    return null;
            }

            if (targetArray == null || targetArray.Length == 0)
            {
                Debug.LogWarning("卡牌数组为空！");
                return null;
            }

            // 构建可用列表：需要同时满足全局已选规则 和 本次三选的不重复规则
            List<CardConfigEX> available = new List<CardConfigEX>();

            foreach (var card in targetArray)
            {
                // Stackable为false的卡牌，如果已在全局记录或本次已选中，则排除
                if (!card.Stackable)
                {
                    if (
                        selectedEXCardNames.Contains(card.Name)
                        || extraExcludeNames.Contains(card.Name)
                    )
                        continue;
                }
                else
                {
                    // Stackable为true的卡牌，只需要排除本次已选中的
                    if (extraExcludeNames.Contains(card.Name))
                        continue;
                }

                available.Add(card);
            }

            if (available.Count == 0)
            {
                Debug.LogWarning("没有可用的卡牌！");
                return null;
            }

            return available[Random.Range(0, available.Count)];
        }

        /// <summary>
        /// 重置已选卡牌记录（比如开始新一局游戏时调用）
        /// </summary>
        public void ResetEXCardSelection()
        {
            selectedEXCardNames.Clear();
        }
    }
}