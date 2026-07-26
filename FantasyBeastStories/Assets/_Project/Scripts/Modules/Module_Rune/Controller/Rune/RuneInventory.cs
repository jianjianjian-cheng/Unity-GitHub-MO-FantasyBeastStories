using System.Collections.Generic;
using UI.Framework.Utils;
using UI.RedDot;
using UnityEngine;
using Managers;

namespace Controllers.Rune
{
    /// <summary>
    /// 局外符文背包 — 存储玩家已收集的符文 ID。
    /// 数据由 SaveManager 统一持久化（save.json），运行时仅维护内存 List。
    /// 拾取符文时调用 AddRune()，在 RunePanel 或大厅中可读取 GetAllRuneIds() 展示。
    /// </summary>
    public static class RuneInventory
    {
        private static List<int> _collectedIds = new List<int>();

        /// <summary>每个 runeId 尚未查看的新增数量</summary>
        private static Dictionary<int, int> _newCounts = new Dictionary<int, int>();

        /// <summary>获取所有已收集的符文 ID</summary>
        public static List<int> GetAllRuneIds()
        {
            return new List<int>(_collectedIds);
        }

        /// <summary>添加一个符文到背包（不去重，支持重复购买）</summary>
        public static void AddRune(int runeId)
        {
            _collectedIds.Add(runeId);

            if (!_newCounts.ContainsKey(runeId))
                _newCounts[runeId] = 0;
            _newCounts[runeId]++;

            // 激活符文导航按钮红点
            ServiceLocator.Get<RedDotController>()?.ActivateRedDot(RedDotKeys.RuneNew);

            Debug.Log($"[RuneInventory] 收集符文 ID={runeId}，当前背包共 {_collectedIds.Count} 个，新红点剩余 {_newCounts[runeId]}");
        }

        /// <summary>获取指定 runeId 的新增（未查看）数量</summary>
        public static int GetNewCount(int runeId)
        {
            return _newCounts.TryGetValue(runeId, out var count) ? count : 0;
        }

        /// <summary>
        /// 消费一个新增名额。当所有 runeId 的新增数量都归零时，
        /// 自动关闭符文导航按钮红点。
        /// </summary>
        public static void ConsumeNew(int runeId)
        {
            if (!_newCounts.TryGetValue(runeId, out var count) || count <= 0)
                return;

            _newCounts[runeId] = count - 1;

            // 检查是否全部归零
            bool anyRemaining = false;
            foreach (var kvp in _newCounts)
            {
                if (kvp.Value > 0)
                {
                    anyRemaining = true;
                    break;
                }
            }

            if (!anyRemaining)
                ServiceLocator.Get<RedDotController>()?.MarkAsRead(RedDotKeys.RuneNew);
        }

        /// <summary>检查是否已拥有某个符文</summary>
        public static bool HasRune(int runeId) => _collectedIds.Contains(runeId);

        /// <summary>
        /// 分解所有重复符文，每种只保留一个。
        /// </summary>
        /// <returns>被分解的符文数量</returns>
        public static int BreakdownDuplicates()
        {
            var seen = new HashSet<int>();
            var kept = new List<int>();
            int removed = 0;

            foreach (int id in _collectedIds)
            {
                if (seen.Add(id))
                    kept.Add(id);
                else
                    removed++;
            }

            _collectedIds = kept;
            Debug.Log($"[RuneInventory] 分解完成：→ {kept.Count}，分解了 {removed} 个重复符文");
            return removed;
        }

        /// <summary>从存档恢复符文背包（由 SaveManager 读档时调用）</summary>
        public static void RestoreFromSave(List<int> ids)
        {
            _collectedIds = ids != null ? new List<int>(ids) : new List<int>();
            Debug.Log($"[RuneInventory] 从存档恢复 {_collectedIds.Count} 个符文");
        }

        /// <summary>清空背包（测试用）</summary>
        public static void Clear()
        {
            _collectedIds.Clear();
            _newCounts.Clear();
            Debug.Log("[RuneInventory] 背包已清空");
        }
    }
}
