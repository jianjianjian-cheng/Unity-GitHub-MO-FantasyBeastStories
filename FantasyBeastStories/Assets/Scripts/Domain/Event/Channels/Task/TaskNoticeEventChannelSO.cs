using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.Task
{
    [CreateAssetMenu(menuName = "Events/Task/Task Notice Event Channel")]
    public class TaskNoticeEventChannelSO : BaseEventChannelSO<TaskNoticeData>
    {
    }

    public class TaskNoticeData : EventArgsBase
    {
        public string name;
        public string description;
        public int limitTime;
        public int requiredCount;

        public TaskNoticeData(string name, string description, int limitTime, int requiredCount)
        {
            this.name = name;
            this.description = description;
            this.limitTime = limitTime;
            this.requiredCount = requiredCount;
        }
    }
}
