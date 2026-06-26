using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.UI
{
    [CreateAssetMenu(menuName = "Events/UI/Health Update Event Channel")]
    public class HealthUpdateEventChannelSO : BaseEventChannelSO<HealthUpdateData>
    {
    }
}
