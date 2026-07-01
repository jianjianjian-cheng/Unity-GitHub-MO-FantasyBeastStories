using System.Collections.Generic;
using Domain.Event;
using UnityEngine;

namespace Application
{
    /// <summary>
    /// 任务进度管理器 — 对局中记录事件，对局结束时累加保存。
    ///
    /// 职责：
    /// - 对局中通过 RecordKill / RecordDamage 等记录事件
    /// - 对局结束调用 FinalizeTasks()，将本次进度合并到持久化存储
    /// - 打开任务面板时读取 QuestTaskInventory 获取累计进度
    ///
    /// 设计说明：
    /// - 纯本地记录，无需网络同步
    /// - 对局中只暂存在 pendingProgress，对局结束后才写入磁盘
    /// - 面板打开时从磁盘读取最新进度并播放平滑过渡动画
    /// </summary>
    public class QuestTaskManager : MonoBehaviour
    {
        public static QuestTaskManager Instance { get; private set; }

        // 对局中暂存的进度（taskId → 本次累加次数）
        private Dictionary<int, int> pendingProgress = new Dictionary<int, int>();

        // 任务数据库
        private QuestTaskDatabaseSO taskDatabase;

        // ──────────────────────────────────
        //  单例生命周期
        // ──────────────────────────────────

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            taskDatabase = Resources.Load<QuestTaskDatabaseSO>("QuestTask/QuestTaskDatabase");
            if (taskDatabase == null)
                Debug.LogWarning("[QuestTaskManager] 未找到 QuestTaskDatabase，请在 Resources/QuestTask/ 目录下创建");
        }

        // ──────────────────────────────────
        //  对局中记录（由其他系统调用）
        // ──────────────────────────────────

        /// <summary>记录一次击杀</summary>
        public void RecordKill() => AddProgress(QuestTaskType.KillEnemy);

        /// <summary>记录伤害（每100点伤害计1次）</summary>
        public void RecordDamage(int damageAmount)
        {
            // 按每100伤害为单位累加，避免数值过大
            int count = Mathf.Max(1, damageAmount / 100);
            for (int i = 0; i < count; i++)
                AddProgress(QuestTaskType.DealDamage);
        }

        /// <summary>记录一次金币获得</summary>
        public void RecordCoin() => AddProgress(QuestTaskType.CollectCoin);

        /// <summary>记录一次经验获得</summary>
        public void RecordExp() => AddProgress(QuestTaskType.CollectExp);

        /// <summary>记录完成一局</summary>
        public void RecordMatchComplete() => AddProgress(QuestTaskType.CompleteMatch);

        private void AddProgress(QuestTaskType type)
        {
            if (taskDatabase == null || taskDatabase.allTasks == null) return;

            foreach (var task in taskDatabase.allTasks)
            {
                if (task != null && task.taskType == type)
                {
                    if (!pendingProgress.ContainsKey(task.taskId))
                        pendingProgress[task.taskId] = 0;
                    pendingProgress[task.taskId]++;
                }
            }
        }

        // ──────────────────────────────────
        //  对局结算
        // ──────────────────────────────────

        /// <summary>
        /// 对局结束：将本次 pendingProgress 合并到持久化存储。
        /// 由 MatchStatisticsManager.FinalizeMatch() 或 GameManager 在返回大厅时调用。
        /// </summary>
        public void FinalizeTasks()
        {
            if (pendingProgress.Count == 0) return;

            var savedProgress = QuestTaskInventory.LoadAllProgress();

            foreach (var kvp in pendingProgress)
            {
                int taskId = kvp.Key;
                int added = kvp.Value;

                if (savedProgress.ContainsKey(taskId))
                    savedProgress[taskId] += added;
                else
                    savedProgress[taskId] = added;

                QuestTaskInventory.SaveProgress(taskId, savedProgress[taskId]);

                Debug.Log($"[QuestTaskManager] 任务 {taskId} 进度 +{added}，当前累计 {savedProgress[taskId]}");
            }

            pendingProgress.Clear();
        }

        /// <summary>新对局开始时调用，清空暂存进度</summary>
        public void ResetPendingProgress()
        {
            pendingProgress.Clear();
        }

        /// <summary>获取所有任务的最新进度（从持久化存储读取）</summary>
        public Dictionary<int, int> GetAllProgress()
        {
            return QuestTaskInventory.LoadAllProgress();
        }
    }
}