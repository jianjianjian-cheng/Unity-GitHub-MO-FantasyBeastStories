using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 任务数据类
[System.Serializable]
public class KillTask
{
    public string TaskId;
    public Vector3 ZoneCenter;
    public float ZoneRadius;
    public int RequiredKills;
    public int CurrentKills;
    public bool IsCompleted;
    
    public KillTask(string id, Vector3 center, float radius, int required)
    {
        TaskId = id;
        ZoneCenter = center;
        ZoneRadius = radius;
        RequiredKills = required;
        CurrentKills = 0;
        IsCompleted = false;
    }
}
