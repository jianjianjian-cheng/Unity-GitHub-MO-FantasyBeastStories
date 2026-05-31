using System.Collections;
using System.Collections.Generic;
using CardData;
using UnityEngine;

namespace MyNamespace
{
    [CreateAssetMenu(fileName = "CardDatabase", menuName = "公用卡牌数据库")]
    public class CardDatabasePublic : ScriptableObject
    {
        public CardConfigPublicNormal[] cardsPublicNormal;
        public CardConfigPublicEpic[] cardsPublicEpic;
        public CardConfigPublicLegend[] cardsPublicLegend;
    }
}
