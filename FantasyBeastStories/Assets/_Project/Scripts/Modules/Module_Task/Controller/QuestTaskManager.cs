using System.Collections.Generic;
using Core;
using Core.SharedModel;
using Core.Save;
using UnityEngine;
using Controllers.Task;

namespace Controllers.Task
{
    /// <summary>
    /// 任务进度控制器 — 薄层 MonoBehaviour，持有 QuestTaskModel 实例。
    ///
    /// 职责：
    /// - 生命周期管理（单例 + DontDestroyOnLoad）
    /// - 存档注册（ISaveable）
    /// - 从 QuestTaskDatabaseSO 查询匹配的 taskId 列表
    /// - 业务逻辑委托给 QuestTaskModel
    /// </summary>
    public class QuestTaskManager : MonoBehaviour, ISaveable
    {
        private static QuestTaskManager _instance;

        /// <summary>任务进度模型实例（纯 C#，可单测）</summary>
        public QuestTaskModel Model { get; private set; }

        private QuestTaskDatabaseSO _taskDatabase;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            ServiceLocator.Register(this);
            DontDestroyOnLoad(gameObject);
            Model = new QuestTaskModel();
        }

        void Start()
        {
            ServiceLocator.Get<SaveManager>()?.RegisterSaveable(this);
            _taskDatabase = AssetLoader.LoadAsset<QuestTaskDatabaseSO>("Lobby_QuestTask_QuestTaskDatabase");
            if (_taskDatabase == null)
                Debug.LogWarning("[QuestTaskManager] 未找到 QuestTaskDatabase");
        }

        // ========== ISaveable 实现 ==========

        public string SaveId => "QuestTaskManager";

        public void OnSave(SaveData data) => data.taskProgress = Model.GetAllProgress();
        public void OnLoad(SaveData data) => Model.SetAllProgress(data.taskProgress);

        // ========== 对局中记录 ==========

        public void RecordKill() => AddProgressByType(QuestTaskType.KillEnemy);

        public void RecordDamage(int damageAmount)
        {
            int count = Mathf.Max(1, damageAmount / 100);
            for (int i = 0; i < count; i++)
                AddProgressByType(QuestTaskType.DealDamage);
        }

        public void RecordCoin() => AddProgressByType(QuestTaskType.CollectCoin);
        public void RecordExp() => AddProgressByType(QuestTaskType.CollectExp);
        public void RecordMatchComplete() => AddProgressByType(QuestTaskType.CompleteMatch);

        private void AddProgressByType(QuestTaskType type)
        {
            if (_taskDatabase == null || _taskDatabase.allTasks == null)
            {
                Debug.LogWarning($"[QuestTaskManager] AddProgress({type}) 跳过：taskDatabase 未加载");
                return;
            }

            var taskIds = new List<int>();
            foreach (var task in _taskDatabase.allTasks)
            {
                if (task != null && task.taskType == type)
                    taskIds.Add(task.taskId);
            }

            Model.AddProgress(taskIds);
        }

        // ========== 局外结算 ==========

        public QuestSettleResult SettlePendingProgress()
        {
            var result = Model.SettlePendingProgress();

            // 合并后立即存档
            ServiceLocator.Get<SaveManager>()?.SaveGame();

            return result;
        }

        // ========== 便捷转发（向后兼容） ==========

        public void ResetPendingProgress() => Model.ResetPendingProgress();
        public Dictionary<int, int> GetAllProgress() => Model.GetAllProgress();
        public void SetAllProgress(Dictionary<int, int> progress) => Model.SetAllProgress(progress);

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                ServiceLocator.Unregister<QuestTaskManager>();
            }
        }
    }
}
