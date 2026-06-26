using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.UI
{
    [CreateAssetMenu(menuName = "Events/UI/Task UI Event Channel")]
    public class TaskUIEventChannelSO : BaseEventChannelSO<TaskUIUpdateData>
    {
    }
}
