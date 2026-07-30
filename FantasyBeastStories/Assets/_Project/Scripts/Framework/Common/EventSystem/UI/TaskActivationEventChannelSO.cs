using Core.Channels.Base;
using UnityEngine;
using Core.SharedModel;

namespace Core.Channels.Task
{
    [CreateAssetMenu(menuName = "Events/Task/Task Activation Event Channel")]
    public class TaskActivationEventChannelSO : BaseEventChannelSO<TaskBase>
    {
    }
}