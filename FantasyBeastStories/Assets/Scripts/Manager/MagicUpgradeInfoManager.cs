using System.Collections.Generic;
using CardData;
using MyNamespace;
using UnityEngine;

namespace Manager
{
    public class MagicUpgradeInfoManager : MonoBehaviour
    {
        #region 单例

        public static MagicUpgradeInfoManager instance;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion

        public CardDatabasePublic cardDatabasePublic;
        public CardConfigPublicNormal[] cardsPublicNormal;
        public CardConfigPublicEpic[] cardsPublicEpic;
        public CardConfigPublicLegend[] cardsPublicLegend;
        public int luckRate = 10;

        private void Start()
        {
            LocalCard();
        }

        private void LocalCard()
        {
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
    }
}
