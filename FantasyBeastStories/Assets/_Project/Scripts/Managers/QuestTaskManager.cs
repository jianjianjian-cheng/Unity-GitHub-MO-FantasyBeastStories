using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// 结算结果：打开任务面板时由 SettlePendingProgress 返回，
    /// 供 UI 层驱动平滑过渡动画。
    /// </summary>
    public class QuestSettleResult
    {
        /// <summary>结算前保存的旧进度（基准值）</summary>
        public Dictionary<int, int> oldProgress;
        /// <summary>本局新增的进度（delta，用于驱动动画）</summary>
        public Dictionary<int, int> delta;
    }

    /// <summary>
    /// 任务进度管理器 — 对局中记录事件，对局结束时缓存，打开面板时合并。
    ///
    /// 职责：
    /// - 对局中通过 RecordKill / RecordDamage 等记录事件
    /// - 打开任务面板时由 MassionPanel 调用 SettlePendingProgress()
    ///   → 将本次进度合并到累计进度 → 存档 → 返回 delta 供动画
    /// - 对局中只暂存在 pendingProgress，面板打开后才写入磁盘
    /// - 面板打开时从磁盘读取最新进度并播放平滑过渡动画
    /// </summary>
    public class QuestTaskManager : MonoBehaviour
    {
        public static QuestTaskManager Instance { get; private set; }

        // 对局中暂存的进度（taskId → 本次累加次数）
        private Dictionary<int, int> pendingProgress = new Dictionary<int, int>();

        // 跨对局累计进度（由 SaveManager 统一读写，替代 QuestTaskInventory）
        private Dictionary<int, int> cumulativeProgress = new Dictionary<int, int>();

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
            if (taskDatabase == null || taskDatabase.allTasks == null)
            {
                Debug.LogWarning($"[QuestTaskManager] AddProgress({type}) 跳过：taskDatabase={taskDatabase == null}");
                return;
            }

            int matched = 0;
            foreach (var task in taskDatabase.allTasks)
            {
                if (task != null && task.taskType == type)
                {
                    if (!pendingProgress.ContainsKey(task.taskId))
                        pendingProgress[task.taskId] = 0;
                    pendingProgress[task.taskId]++;
                    matched++;
                }
            }
            Debug.Log($"[QuestTaskManager] AddProgress({type}) 匹配 {matched} 个任务，pendingProgress 当前共 {pendingProgress.Count} 个条目");
        }

        // ──────────────────────────────────
        //  局外结算（由 MassionPanel 打开时调用）
        // ──────────────────────────────────

        /// <summary>
        /// 打开任务面板时调用：合并 pendingProgress → cumulativeProgress，
        /// 保存存档，返回旧进度和 delta 供 UI 驱动平滑动画。
        /// </summary>
        public QuestSettleResult SettlePendingProgress()
        {
            Debug.Log($"[QuestTaskManager] SettlePendingProgress：pending 共 {pendingProgress.Count} 项，cumulative 共 {cumulativeProgress.Count} 项");

            // 1. 快照旧进度（结算前的存档值）
            var oldSnapshot = new Dictionary<int, int>(cumulativeProgress);

            // 2. 合并本局新增进度
            var delta = new Dictionary<int, int>();
            foreach (var kvp in pendingProgress)
            {
                int taskId = kvp.Key;
                int added = kvp.Value;

                if (cumulativeProgress.ContainsKey(taskId))
                    cumulativeProgress[taskId] += added;
                else
                    cumulativeProgress[taskId] = added;

                delta[taskId] = added;

                Debug.Log($"[QuestTaskManager] 任务 {taskId} 进度 +{added}，当前累计 {cumulativeProgress[taskId]}");
            }

            // 3. 存入磁盘
            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveGame();

            // 4. 清空暂存
            pendingProgress.Clear();

            return new QuestSettleResult
            {
                oldProgress = oldSnapshot,
                delta = delta
            };
        }

        // ──────────────────────────────────
        //  对局开始/结束
        // ──────────────────────────────────

        /// <summary>新对局开始时调用，清空暂存进度</summary>
        public void ResetPendingProgress()
        {
            pendingProgress.Clear();
        }

        /// <summary>获取所有任务的累计进度（给 SaveManager 存档时调用）</summary>
        public Dictionary<int, int> GetAllProgress()
        {
            return cumulativeProgress;
        }

        #region 公共方法
        /// <summary>
        /// 从存档恢复所有任务进度（由 SaveManager 读档时调用）
        /// </summary>
        public void SetAllProgress(Dictionary<int, int> progress)
        {
            cumulativeProgress.Clear();
            if (progress == null) return;

            foreach (var kvp in progress)
            {
                cumulativeProgress[kvp.Key] = kvp.Value;
            }

            Debug.Log($"[QuestTaskManager] 从存档恢复 {cumulativeProgress.Count} 个任务进度");
        }
        #endregion
    }
}