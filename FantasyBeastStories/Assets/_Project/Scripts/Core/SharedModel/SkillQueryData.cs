using Core;

namespace Core.SharedModel
{
    public enum SkillQueryType
    {
        GetMaxAttackCount,
        GetLuckRate,
        GetRandomEXCard,
        GetThreeRandomEXCards,
        GetThreeRandomCards,
        AddLuckRate,
        GetUpgradeExperience
    }

    public class SkillQueryData : EventArgsBase
    {
        public SkillQueryType queryType;
        public string cardType;
        public int intValue;
        public CardConfigSO[] cardsResult;
        public CardConfigSO cardResult;

        public SkillQueryData(SkillQueryType queryType)
        {
            this.queryType = queryType;
        }

        public SkillQueryData(SkillQueryType queryType, string cardType)
        {
            this.queryType = queryType;
            this.cardType = cardType;
        }

        public SkillQueryData(SkillQueryType queryType, int intValue)
        {
            this.queryType = queryType;
            this.intValue = intValue;
        }
    }
}
