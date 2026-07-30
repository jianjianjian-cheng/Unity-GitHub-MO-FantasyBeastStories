using System.Collections.Generic;
using UnityEngine;

namespace Core.SharedModel
{
    /// <summary>
    /// 任务模型 — 纯 C# 类，不继承 MonoBehaviour，可独立单元测试。
    ///
    /// 持有：
    /// - 活动任务字典（taskId → TaskBase）
    /// - 已上报敌人 ViewID 集合（防止重复计数）
    /// - 任务进度记录（击杀/护送计数）
    ///
    /// 外部依赖（RPC / Coroutine / GameObject / EventChannelSO）
    /// 由 Controller 处理，Model 只管理数据与回调通知。
    /// </summary>
    public class TaskModel
    {
        private readonly Dictionary<string, TaskBase> _tasks = new();
        private readonly HashSet<int> _reportedEnemies = new();

        /// <summary>任务进度变化回调</summary>
        public event System.Action<TaskBase> OnTaskUpdated;
        /// <summary>任务完成回调</summary>
        public event System.Action<TaskBase> OnTaskCompleted;

        // ──────────────────────────────────
        //  任务管理
        // ──────────────────────────────────

        public IReadOnlyDictionary<string, TaskBase> Tasks => _tasks;

        public void ClearTasks() => _tasks.Clear();

        public void AddTask(string taskId, TaskBase task) => _tasks[taskId] = task;

        public bool TryGetTask(string taskId, out TaskBase task) => _tasks.TryGetValue(taskId, out task);

        // ──────────────────────────────────
        //  击杀上报去重
        // ──────────────────────────────────

        public bool HasReported(int enemyViewID) => _reportedEnemies.Contains(enemyViewID);

        public void MarkReported(int enemyViewID) => _reportedEnemies.Add(enemyViewID);

        public void ClearReported() => _reportedEnemies.Clear();

        // ──────────────────────────────────
        //  任务进度更新
        // ──────────────────────────────────

        /// <summary>
        /// 上报击杀位置，检查是否在任务区域内。
        /// 返回需要网络同步的结果（taskId, currentCount, isCompleted），未匹配则返回 null。
        /// </summary>
        public TaskProgressUpdate ReportKill(Vector3 killPosition, int enemyViewID, EnemyReportType reportType)
        {
            if (HasReported(enemyViewID))
                return null;

            foreach (var task in _tasks.Values)
            {
                if (task.IsCompleted)
                    continue;

                if (task is KillTask killTask)
                {
                    if (reportType != EnemyReportType.Kill)
                        continue;

                    if (Vector3.Distance(killPosition, task.ZoneCenter) <= task.ZoneRadius)
                    {
                        killTask.CurrentKills++;
                        MarkReported(enemyViewID);

                        if (killTask.CurrentKills >= killTask.RequiredKills)
                            task.IsCompleted = true;

                        return new TaskProgressUpdate(
                            task.TaskId, killTask.CurrentKills, task.IsCompleted);
                    }
                }
                else if (task is EscortTask escortTask)
                {
                    if (reportType != EnemyReportType.EscortArrive)
                        continue;

                    if (Vector3.Distance(killPosition, task.ZoneCenter) <= task.ZoneRadius)
                    {
                        escortTask.currentEscorts++;
                        MarkReported(enemyViewID);

                        if (escortTask.currentEscorts >= escortTask.requiredEscorts)
                            task.IsCompleted = true;

                        return new TaskProgressUpdate(
                            task.TaskId, escortTask.currentEscorts, task.IsCompleted);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 从 RPC 同步更新任务进度。
        /// 返回 true 表示任务完成（新完成）。
        /// </summary>
        public bool UpdateProgress(string taskId, int count, bool completed)
        {
            if (!_tasks.TryGetValue(taskId, out var task))
                return false;

            switch (task)
            {
                case KillTask killTask:
                    killTask.CurrentKills = count;
                    killTask.IsCompleted = completed;
                    break;
                case EscortTask escortTask:
                    escortTask.currentEscorts = count;
                    escortTask.IsCompleted = completed;
                    break;
            }

            OnTaskUpdated?.Invoke(task);

            if (completed)
                OnTaskCompleted?.Invoke(task);

            return completed;
        }

        /// <summary>获取所有活动任务（供 Gizmos 绘制）</summary>
        public IEnumerable<TaskBase> GetAllTasks() => _tasks.Values;
    }

    /// <summary>任务进度更新结果（供 Controller 发 RPC）</summary>
    public class TaskProgressUpdate
    {
        public string TaskId;
        public int Count;
        public bool IsCompleted;

        public TaskProgressUpdate(string taskId, int count, bool isCompleted)
        {
            TaskId = taskId;
            Count = count;
            IsCompleted = isCompleted;
        }
    }
}
