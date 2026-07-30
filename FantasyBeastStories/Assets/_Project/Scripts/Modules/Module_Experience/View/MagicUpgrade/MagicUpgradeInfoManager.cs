using System.Collections.Generic;
using Controllers.Card;
using Core;
using Core.Channels.Player;
using UnityEngine;
using Core.SharedModel;

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

        public CardDatabaseSO cardDatabase;
        private List<CardConfigSO> publicNormal;
        private List<CardConfigSO> publicEpic;
        private List<CardConfigSO> publicLegend;
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
            if (cardDatabase == null)
            {
                Debug.LogWarning($"MagicUpgradeInfoManager: cardDatabase is not assigned in Inspector on {gameObject.name}");
                return;
            }
            publicNormal = cardDatabase.GetPublicCards(CardQuality.Normal);
            publicEpic = cardDatabase.GetPublicCards(CardQuality.Epic);
            publicLegend = cardDatabase.GetPublicCards(CardQuality.Legend);
        }

        /// <summary>
        /// 获取三张不重复的卡牌，不固定品质位置
        /// </summary>
        public CardConfigSO[] GetThreeRandomCards()
        {
            CardConfigSO[] result = new CardConfigSO[3];
            HashSet<string> selectedNames = new HashSet<string>();

            for (int i = 0; i < 3; i++)
            {
                string rolledQuality = RollQualityByLuck();
                CardConfigSO card = GetRandomCardByQuality(rolledQuality, selectedNames);

                if (card == null)
                    card = GetAnyAvailableCard(selectedNames);

                if (card != null)
                {
                    result[i] = card;
                    selectedNames.Add(card.cardName);
                }
            }

            return result;
        }

        /// <summary>
        /// 根据品质获取随机卡牌
        /// </summary>
        private CardConfigSO GetRandomCardByQuality(string quality, HashSet<string> excludeNames)
        {
            switch (quality)
            {
                case "普通":
                    return GetOneFromPool(publicNormal, excludeNames);
                case "史诗":
                    return GetOneFromPool(publicEpic, excludeNames);
                case "传说":
                    return GetOneFromPool(publicLegend, excludeNames);
            }
            return null;
        }

        /// <summary>
        /// 从指定池子随机取一张未被选中的
        /// </summary>
        private CardConfigSO GetOneFromPool(List<CardConfigSO> pool, HashSet<string> excludeNames)
        {
            if (pool == null || pool.Count == 0)
                return null;

            var available = new List<CardConfigSO>();
            foreach (var card in pool)
            {
                if (!excludeNames.Contains(card.cardName))
                    available.Add(card);
            }

            if (available.Count == 0)
                return null;
            return available[Random.Range(0, available.Count)];
        }

        /// <summary>
        /// 兜底：从所有剩余卡牌中随便取一张
        /// </summary>
        private CardConfigSO GetAnyAvailableCard(HashSet<string> excludeNames)
        {
            var all = new List<CardConfigSO>();
            AddPoolToList(all, publicNormal, excludeNames);
            AddPoolToList(all, publicEpic, excludeNames);
            AddPoolToList(all, publicLegend, excludeNames);

            if (all.Count == 0)
                return null;
            return all[Random.Range(0, all.Count)];
        }

        private void AddPoolToList(List<CardConfigSO> list, List<CardConfigSO> pool, HashSet<string> excludeNames)
        {
            if (pool == null) return;
            foreach (var card in pool)
            {
                if (!excludeNames.Contains(card.cardName))
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

            if (roll < normalChance) return "普通";
            else if (roll < normalChance + epicChance) return "史诗";
            else return "传说";
        }

        // ── 专属卡牌 ──
        private HashSet<string> selectedEXCardNames = new HashSet<string>();

        /// <summary>
        /// 从 CardDatabase 中随机抽取一张专属卡
        /// </summary>
        public CardConfigSO GetRandomEXCard(string cardType)
        {
            if (cardDatabase == null)
            {
                Debug.LogWarning("CardDatabase 未赋值！");
                return null;
            }

            var cards = cardDatabase.GetExclusiveCards(cardType);
            var available = new List<CardConfigSO>();
            foreach (var card in cards)
            {
                if (!card.stackable && selectedEXCardNames.Contains(card.cardName))
                    continue;
                available.Add(card);
            }

            if (available.Count == 0)
            {
                Debug.LogWarning($"没有可用的 {cardType} 专属卡！");
                return null;
            }

            var picked = available[Random.Range(0, available.Count)];
            selectedEXCardNames.Add(picked.cardName);
            return picked;
        }

        /// <summary>
        /// 获取三张不重复的专属卡牌
        /// </summary>
        public CardConfigSO[] GetThreeRandomEXCards(string cardType)
        {
            CardConfigSO[] result = new CardConfigSO[3];
            HashSet<string> currentSelection = new HashSet<string>();

            for (int i = 0; i < 3; i++)
            {
                var card = GetRandomEXCardInternal(cardType, currentSelection);
                if (card != null)
                {
                    result[i] = card;
                    currentSelection.Add(card.cardName);
                    selectedEXCardNames.Add(card.cardName);
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        private CardConfigSO GetRandomEXCardInternal(string cardType, HashSet<string> extraExclude)
        {
            if (cardDatabase == null) return null;
            var cards = cardDatabase.GetExclusiveCards(cardType);
            var available = new List<CardConfigSO>();
            foreach (var card in cards)
            {
                if (!card.stackable && (selectedEXCardNames.Contains(card.cardName) || extraExclude.Contains(card.cardName)))
                    continue;
                if (card.stackable && extraExclude.Contains(card.cardName))
                    continue;
                available.Add(card);
            }

            if (available.Count == 0) return null;
            return available[Random.Range(0, available.Count)];
        }

        public void ResetEXCardSelection()
        {
            selectedEXCardNames.Clear();
        }
    }
}