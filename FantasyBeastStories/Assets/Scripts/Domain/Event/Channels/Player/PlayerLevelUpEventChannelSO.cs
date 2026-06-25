using UnityEngine;
using Domain.Event.Channels.Base;

namespace Domain.Event.Channels.Player
{
    [CreateAssetMenu(menuName = "Events/Player/Level Up Channel")]
    public class PlayerLevelUpEventChannelSO : BaseEventChannelSO<int> { }
}
