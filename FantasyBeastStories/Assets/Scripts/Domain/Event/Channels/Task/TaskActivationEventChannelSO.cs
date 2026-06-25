using Domain.Event.Channels.Base;
using Domain.Task;
using UnityEngine;

namespace Domain.Event.Channels.Task
{
    [CreateAssetMenu(menuName = "Events/Task/Task Activation Event Channel")]
    public class TaskActivationEventChannelSO : BaseEventChannelSO<TaskBase>
    {
    }
}