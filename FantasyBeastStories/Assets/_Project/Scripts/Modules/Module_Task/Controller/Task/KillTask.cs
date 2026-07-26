using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Controllers.Task
{
    [System.Serializable]
    public class KillTask : TaskBase
    {
        public int RequiredKills;
        public int CurrentKills;

        public KillTask(string id, Vector3 center, float radius, int required, int limitTime)
        {
            TaskId = id;
            ZoneCenter = center;
            ZoneRadius = radius;
            RequiredKills = required;
            CurrentKills = 0;
            IsCompleted = false;
            this.limitTime = limitTime;
        }
    }
}
