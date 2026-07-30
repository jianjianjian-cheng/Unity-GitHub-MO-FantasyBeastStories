using Core.Channels.Base;
using Core.SharedModel;
using UnityEngine;

namespace Core.Channels.Player
{
  [CreateAssetMenu(menuName = "Events/Player/Skill Query Event Channel")]
  public class SkillQueryEventChannelSO : BaseEventChannelSO<SkillQueryData>
  {
  }
}
