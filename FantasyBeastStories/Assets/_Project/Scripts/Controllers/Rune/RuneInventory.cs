using System.Collections.Generic;
using UnityEngine;

namespace Controllers.Rune
{
    /// <summary>
    /// 局外符文背包 — 存储玩家已收集的符文 ID，跨局持久化（PlayerPrefs）。
    /// 拾取符文时调用 AddRune()，在 RunePanel 或大厅中可读取 GetAllRuneIds() 展示。
    /// </summary>
    public static class RuneInventory
    {
        private const string PlayerPrefsKey = "RuneInventory_CollectedIds";

        /// <summary>获取所有已收集的符文 ID</summary>
        public static List<int> GetAllRuneIds()
        {
            var ids = new List<int>();
            string saved = PlayerPrefs.GetString(PlayerPrefsKey, "");
            if (string.IsNullOrEmpty(saved)) return ids;

            foreach (var part in saved.Split(','))
            {
                if (int.TryParse(part, out int id))
                    ids.Add(id);
            }
            return ids;
        }

        /// <summary>添加一个符文到背包（不去重，支持重复购买）</summary>
        public static void AddRune(int runeId)
        {
            var ids = GetAllRuneIds();
            ids.Add(runeId);
            Save(ids);
            Debug.Log($"[RuneInventory] 收集符文 ID={runeId}，当前背包共 {ids.Count} 个");
        }

        /// <summary>检查是否已拥有某个符文</summary>
        public static bool HasRune(int runeId) => GetAllRuneIds().Contains(runeId);

        /// <summary>
        /// 分解所有重复符文，每种只保留一个。
        /// </summary>
        /// <returns>被分解的符文数量</returns>
        public static int BreakdownDuplicates()
        {
            var ids = GetAllRuneIds();
            var seen = new HashSet<int>();
            var kept = new List<int>();
            int removed = 0;

            foreach (int id in ids)
            {
                if (seen.Add(id))
                    kept.Add(id);
                else
                    removed++;
            }

            Save(kept);
            Debug.Log($"[RuneInventory] 分解完成：{ids.Count} → {kept.Count}，分解了 {removed} 个重复符文");
            return removed;
        }

        /// <summary>清空背包（测试用）</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
            Debug.Log("[RuneInventory] 背包已清空");
        }

        private static void Save(List<int> ids)
        {
            PlayerPrefs.SetString(PlayerPrefsKey, string.Join(",", ids));
            PlayerPrefs.Save();
        }
    }
}