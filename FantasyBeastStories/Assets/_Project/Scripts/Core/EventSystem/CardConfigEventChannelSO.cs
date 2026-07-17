using UnityEngine;
using Core.Channels.Base;
using Controllers.CardData;

namespace Core.Channels
{
  [CreateAssetMenu(menuName = "Events/Card/Card Config Event Channel")]
  public class CardConfigEventChannelSO : BaseEventChannelSO<CardConfigBase> { }
}
