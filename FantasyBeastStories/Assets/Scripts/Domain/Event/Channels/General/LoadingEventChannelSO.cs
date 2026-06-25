using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.General
{
    [CreateAssetMenu(menuName = "Events/General/Loading Event Channel")]
    public class LoadingEventChannelSO : BaseEventChannelSO<bool>
    {
    }
}
