using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Domain.CardData
{
    [CreateAssetMenu(fileName = "CardDatabase", menuName = "公用卡牌数据库")]
    public class CardDatabasePublic : ScriptableObject
    {
        public CardConfigPublicNormal[] cardsPublicNormal;
        public CardConfigPublicEpic[] cardsPublicEpic;
        public CardConfigPublicLegend[] cardsPublicLegend;
    }
}