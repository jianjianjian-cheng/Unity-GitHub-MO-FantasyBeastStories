using UnityEngine;
using Core.Channels.Base;
using Core.SharedModel;

namespace Core.Channels
{
  [CreateAssetMenu(menuName = "Events/Card/Card Config Event Channel")]
  public class CardConfigEventChannelSO : BaseEventChannelSO<CardConfigSO> { }
}
