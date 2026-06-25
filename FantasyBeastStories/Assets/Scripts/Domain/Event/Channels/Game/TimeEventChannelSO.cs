using UnityEngine;
using Domain.Event;
using Domain.Event.Channels.Base;
using Domain.Time.TimeSystem;

namespace Domain.Event.Channels.Game
{
    [CreateAssetMenu(menuName = "Events/Game/Time Event Channel")]
    public class TimeEventChannelSO : BaseEventChannelSO<TimeEventArgs> { }
}
