using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.General
{
    [CreateAssetMenu(menuName = "Events/General/Room Joined Event Channel")]
    public class RoomJoinedEventChannelSO : BaseEventChannelSO<RoomJoinedEventData>
    {
    }
}