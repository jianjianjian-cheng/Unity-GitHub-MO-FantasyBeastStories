using System.Collections.Generic;
using UnityEngine;

namespace Controllers.Task
{
    /// <summary>
    /// 任务进度持久化存储（暂未实现，仅提供接口签名）
    /// TODO: 使用 PlayerPrefs 或存档系统实现持久化
    /// </summary>
    public static class QuestTaskInventory
    {
        private const string PrefsKey = "QuestTask_Progress";

        /// <summary>加载所有任务进度</summary>
        public static Dictionary<int, int> LoadAllProgress()
        {
            // TODO: 从 PlayerPrefs 加载
            // 示例格式：将 "1:15,2:30,3:5" 反序列化为 Dictionary
            return new Dictionary<int, int>();
        }

        /// <summary>保存单个任务进度</summary>
        public static void SaveProgress(int taskId, int count)
        {
            // TODO: 写入 PlayerPrefs
            // 先 LoadAllProgress()，更新对应 taskId，再序列化保存
        }

        /// <summary>清空所有任务进度</summary>
        public static void Clear()
        {
            // TODO: 删除 PlayerPrefs 键
        }
    }
}
