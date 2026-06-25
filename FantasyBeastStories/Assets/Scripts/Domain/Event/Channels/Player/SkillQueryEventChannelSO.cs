using Domain.CardData;
using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.Player
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

  [CreateAssetMenu(menuName = "Events/Player/Skill Query Event Channel")]
  public class SkillQueryEventChannelSO : BaseEventChannelSO<SkillQueryData>
  {
  }

  public class SkillQueryData : EventArgsBase
  {
    public SkillQueryType queryType;
    public string cardType;
    public int intValue;
    public CardConfigBase[] cardsResult;
    public CardConfigBase cardResult;

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