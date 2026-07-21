using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "QuestTask/Task Database")]
public class QuestTaskDatabaseSO : ScriptableObject
{
    public List<QuestTaskSO> allTasks;

    public QuestTaskSO GetTaskById(int id) => allTasks.Find(t => t.taskId == id);

    public bool HasTask(int id) => allTasks.Exists(t => t.taskId == id);
}