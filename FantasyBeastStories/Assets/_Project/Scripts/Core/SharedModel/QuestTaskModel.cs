using System.Collections.Generic;
using UnityEngine;

namespace Core.SharedModel
{
    /// <summary>
    /// 任务进度模型 — 纯 C# 类，不继承 MonoBehaviour，可独立单元测试。
    ///
    /// 持有：
    /// - 对局中暂存进度（pendingProgress）
    /// - 跨对局累计进度（cumulativeProgress）
    ///
    /// 外部依赖（SaveManager / AssetLoader / QuestTaskDatabaseSO）
    /// 由 Controller 处理，Model 只管理数据与结算逻辑。
    /// </summary>
    public class QuestTaskModel
    {
        /// <summary>对局中暂存的进度（taskId → 本次累加次数）</summary>
        private readonly Dictionary<int, int> _pendingProgress = new();

        /// <summary>跨对局累计进度（由 SaveManager 统一读写）</summary>
        private readonly Dictionary<int, int> _cumulativeProgress = new();

        // ──────────────────────────────────
        //  对局中记录
        // ──────────────────────────────────

        /// <summary>
        /// 按任务类型增加暂存进度。
        /// taskIds 由 Controller 从 QuestTaskDatabaseSO 查询后传入。
        /// </summary>
        public void AddProgress(List<int> taskIds)
        {
            if (taskIds == null) return;

            foreach (int taskId in taskIds)
            {
                if (!_pendingProgress.ContainsKey(taskId))
                    _pendingProgress[taskId] = 0;
                _pendingProgress[taskId]++;
            }
        }

        // ──────────────────────────────────
        //  局外结算
        // ──────────────────────────────────

        /// <summary>
        /// 合并 pendingProgress → cumulativeProgress，返回旧进度和 delta。
        /// 调用后清空 pendingProgress。
        /// </summary>
        public QuestSettleResult SettlePendingProgress()
        {
            var oldSnapshot = new Dictionary<int, int>(_cumulativeProgress);
            var delta = new Dictionary<int, int>();

            foreach (var kvp in _pendingProgress)
            {
                int taskId = kvp.Key;
                int added = kvp.Value;

                if (_cumulativeProgress.ContainsKey(taskId))
                    _cumulativeProgress[taskId] += added;
                else
                    _cumulativeProgress[taskId] = added;

                delta[taskId] = added;
            }

            _pendingProgress.Clear();

            return new QuestSettleResult
            {
                oldProgress = oldSnapshot,
                delta = delta
            };
        }

        // ──────────────────────────────────
        //  对局生命周期
        // ──────────────────────────────────

        public void ResetPendingProgress() => _pendingProgress.Clear();

        // ──────────────────────────────────
        //  存档
        // ──────────────────────────────────

        public Dictionary<int, int> GetAllProgress() => _cumulativeProgress;

        public void SetAllProgress(Dictionary<int, int> progress)
        {
            _cumulativeProgress.Clear();
            if (progress == null) return;

            foreach (var kvp in progress)
                _cumulativeProgress[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// 结算结果：打开任务面板时由 SettlePendingProgress 返回，
    /// 供 UI 层驱动平滑过渡动画。
    /// </summary>
    public class QuestSettleResult
    {
        public Dictionary<int, int> oldProgress;
        public Dictionary<int, int> delta;
    }
}
