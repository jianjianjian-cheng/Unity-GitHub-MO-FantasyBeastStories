using Core.Channels.Base;
using UnityEngine;

namespace Core.Channels.UI
{
    [CreateAssetMenu(menuName = "Events/UI/Task UI Event Channel")]
    public class TaskUIEventChannelSO : BaseEventChannelSO<TaskUIUpdateData>
    {
    }
}
