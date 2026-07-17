using Core.Channels.Base;
using UnityEngine;

namespace Core.Channels.UI
{
    [CreateAssetMenu(menuName = "Events/UI/Health Update Event Channel")]
    public class HealthUpdateEventChannelSO : BaseEventChannelSO<HealthUpdateData>
    {
    }
}
