using Core.Channels.Base;
using Controllers.Task;
using UnityEngine;

namespace Core.Channels.Task
{
    [CreateAssetMenu(menuName = "Events/Task/Task Activation Event Channel")]
    public class TaskActivationEventChannelSO : BaseEventChannelSO<TaskBase>
    {
    }
}