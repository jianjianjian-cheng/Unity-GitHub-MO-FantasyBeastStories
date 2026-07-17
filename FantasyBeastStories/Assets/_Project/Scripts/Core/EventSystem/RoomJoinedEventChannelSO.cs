using Core.Channels.Base;
using UnityEngine;

namespace Core.Channels.General
{
    [CreateAssetMenu(menuName = "Events/General/Room Joined Event Channel")]
    public class RoomJoinedEventChannelSO : BaseEventChannelSO<RoomJoinedEventData>
    {
    }
}