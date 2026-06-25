using UnityEngine;
using Domain.Event.Channels.Base;
using Domain.CardData;

namespace Domain.Event.Channels
{
  [CreateAssetMenu(menuName = "Events/Card/Card Config Event Channel")]
  public class CardConfigEventChannelSO : BaseEventChannelSO<CardConfigBase> { }
}
