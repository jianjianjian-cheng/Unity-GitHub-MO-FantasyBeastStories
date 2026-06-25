using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.General
{
    [CreateAssetMenu(menuName = "Events/General/Pool Operation Event Channel")]
    public class PoolOperationEventChannelSO : BaseEventChannelSO<PoolOperationData>
    {
    }
}
