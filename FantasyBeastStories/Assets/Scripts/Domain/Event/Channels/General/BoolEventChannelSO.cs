using UnityEngine;
using Domain.Event.Channels.Base;

namespace Domain.Event.Channels
{
    [CreateAssetMenu(menuName = "Events/General/Bool Event Channel")]
    public class BoolEventChannelSO : BaseEventChannelSO<bool> { }
}
