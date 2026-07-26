using UnityEngine;

namespace Core
{
    public enum TaskUIEventType
    {
        ShowNotice,        // 显示任务通知弹窗
        HideNotice,        // 隐藏任务通知弹窗
        UpdateTime,        // 更新时间显示
        UpdateProgress,    // 更新进度显示
        SetIndicator,      // 设置方向指示器
        ClearIndicator,    // 清除方向指示器
        NoticeData         // 通用数据通知
    }

    public struct TaskUIUpdateData
    {
        public TaskUIEventType eventType;
        public string taskId;
        public string taskName;
        public string description;
        public int limitTime;
        public int requiredCount;
        public string timeString;
        public Vector3 targetPosition;
        public string data;

        // 工厂方法：显示通知
        public static TaskUIUpdateData ShowNotice(string name, string desc, int limit, int count)
        {
            return new TaskUIUpdateData
            {
                eventType = TaskUIEventType.ShowNotice,
                taskName = name,
                description = desc,
                limitTime = limit,
                requiredCount = count
            };
        }

        // 工厂方法：隐藏通知
        public static TaskUIUpdateData HideNotice()
        {
            return new TaskUIUpdateData { eventType = TaskUIEventType.HideNotice };
        }

        // 工厂方法：更新时间
        public static TaskUIUpdateData UpdateTime(string time)
        {
            return new TaskUIUpdateData
            {
                eventType = TaskUIEventType.UpdateTime,
                timeString = time
            };
        }

        // 工厂方法：设置方向指示器
        public static TaskUIUpdateData SetIndicator(Vector3 position, string taskId)
        {
            return new TaskUIUpdateData
            {
                eventType = TaskUIEventType.SetIndicator,
                targetPosition = position,
                taskId = taskId
            };
        }

        // 工厂方法：清除指示器
        public static TaskUIUpdateData ClearIndicator()
        {
            return new TaskUIUpdateData { eventType = TaskUIEventType.ClearIndicator };
        }

        // 工厂方法：通用数据通知
        public static TaskUIUpdateData NoticeData(string data)
        {
            return new TaskUIUpdateData
            {
                eventType = TaskUIEventType.NoticeData,
                data = data
            };
        }
    }
}