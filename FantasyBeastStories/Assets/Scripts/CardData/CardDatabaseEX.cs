using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardData
{
    [CreateAssetMenu(fileName = "CardDatabase", menuName = "小法师卡牌数据库")]
    public class CardDatabaseEX : ScriptableObject
    {
        public CardConfigEX[] cardsEX_WizardBoy;
    }
}
