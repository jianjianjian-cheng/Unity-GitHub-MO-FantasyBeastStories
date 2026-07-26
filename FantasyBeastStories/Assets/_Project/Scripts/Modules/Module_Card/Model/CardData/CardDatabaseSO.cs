using System.Collections.Generic;
using UnityEngine;

namespace Controllers.CardData
{
    /// <summary>
    /// 统一卡牌数据库（ScriptableObject）
    /// 持有所有 CardConfigSO 的引用，提供查询方法
    /// </summary>
    [CreateAssetMenu(menuName = "Config/Card Database")]
    public class CardDatabaseSO : ScriptableObject
    {
        [Tooltip("所有卡牌引用")]
        public List<CardConfigSO> allCards = new List<CardConfigSO>();

        /// <summary>按品质筛选公用卡</summary>
        public List<CardConfigSO> GetPublicCards(CardQuality quality)
        {
            return allCards.FindAll(c => c != null && c.scope == CardScope.Public && c.quality == quality);
        }

        /// <summary>获取所有公用卡</summary>
        public List<CardConfigSO> GetAllPublicCards()
        {
            return allCards.FindAll(c => c != null && c.scope == CardScope.Public);
        }

        /// <summary>按角色获取专属卡</summary>
        public List<CardConfigSO> GetExclusiveCards(string characterType)
        {
            return allCards.FindAll(c => c != null && c.scope == CardScope.Exclusive && c.characterType == characterType);
        }

        /// <summary>根据 cardId 获取卡牌</summary>
        public CardConfigSO GetCardById(string id)
        {
            return allCards.Find(c => c != null && c.cardId == id);
        }
    }
}
