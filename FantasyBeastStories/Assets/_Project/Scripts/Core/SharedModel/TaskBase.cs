using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.SharedModel
{
    [System.Serializable]
    public class TaskBase
    {
        public string TaskId;
        public Vector3 ZoneCenter;
        public float ZoneRadius;
        public bool IsCompleted;
        public int limitTime;
    }
}
