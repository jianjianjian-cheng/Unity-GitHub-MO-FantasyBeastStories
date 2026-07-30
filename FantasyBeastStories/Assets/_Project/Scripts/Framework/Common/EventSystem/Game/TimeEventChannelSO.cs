using UnityEngine;
using Core;
using Core.Channels.Base;
using Core.SharedModel;

namespace Core.Channels.Game
{
    [CreateAssetMenu(menuName = "Events/Game/Time Event Channel")]
    public class TimeEventChannelSO : BaseEventChannelSO<TimeEventArgs> { }
}
