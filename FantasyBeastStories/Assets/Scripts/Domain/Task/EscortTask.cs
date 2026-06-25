using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Domain.Task
{
    [System.Serializable]
    public class EscortTask : TaskBase
    {
        public int requiredEscorts;
        public int currentEscorts;

        public EscortTask(string id, Vector3 center, float radius, int required, int limitTime)
        {
            TaskId = id;
            ZoneCenter = center;
            ZoneRadius = radius;
            requiredEscorts = required;
            currentEscorts = 0;
            IsCompleted = false;
            this.limitTime = limitTime;
        }
    }
}
