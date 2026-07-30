using UnityEngine;

namespace Controllers.Task
{
  [CreateAssetMenu(menuName = "QuestTask/Task Data")]
  public class QuestTaskSO : ScriptableObject
  {
      public int taskId;                  // 唯一标识
      public string taskDescription;      // 任务描述
      public Sprite icon;                 // 任务图标
      public QuestTaskType taskType;      // 任务类型
      public int targetCount;             // 目标值（如 30）
  }

  public enum QuestTaskType
  {
      KillEnemy,          // 击杀怪物
      DealDamage,         // 造成伤害
      CollectCoin,        // 收集金币
      CollectExp,         // 获得经验
      CompleteMatch       // 完成对局
  }
}
