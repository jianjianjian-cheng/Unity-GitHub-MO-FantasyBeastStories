using System.Collections.Generic;
using UnityEngine;

namespace Controllers.Time
{
    /// <summary>
    /// 时间事件列表数据 — ScriptableObject 资源
    /// 将事件列表从场景中的 SyncedGameTimeManager 抽离为独立资源，支持拖拽配置
    /// </summary>
    [CreateAssetMenu(menuName = "Time/TimeEventList", fileName = "TimeEventList")]
    public class TimeEventListSO : ScriptableObject
    {
        [SerializeField]
        private List<TimeEventData> events = new List<TimeEventData>();

        /// <summary>获取事件列表副本（避免修改原始数据）</summary>
        public List<TimeEventData> GetEvents()
        {
            var clonedList = new List<TimeEventData>(events.Count);
            foreach (var evt in events)
            {
                clonedList.Add(evt.Clone());
            }
            return clonedList;
        }

#if UNITY_EDITOR
        /// <summary>编辑器下直接引用原始列表（方便查看）</summary>
        public List<TimeEventData> EditorEvents => events;
#endif
    }
}