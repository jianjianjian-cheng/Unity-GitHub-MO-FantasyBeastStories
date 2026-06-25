using UnityEngine;
using Domain.Event.Channels.Base;

namespace Domain.Event.Channels.Player
{
    [CreateAssetMenu(menuName = "Events/Player/Attribute Change Channel")]
    public class PlayerAttributeChangeEventChannelSO : BaseEventChannelSO<(float, float)> { }
}
