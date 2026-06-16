using System;
using UnityEngine;

namespace Manager.TimeSystem
{
    /// <summary>
    /// 时间事件数据（可网络同步）
    /// </summary>
    [System.Serializable]
    public class TimeEventData
    {
        [Tooltip("唯一标识")]
        public string eventId;

        [Tooltip("事件名称")]
        public string eventName;

        [Tooltip("触发时间（秒）")]
        public float triggerTime;

        [Tooltip("限制时间")]
        public int limittime;

        [Tooltip("任务要求数量")]
        public int requireCount;

        [Tooltip("图标（本地加载，不同步）")]
        public Sprite eventIcon;

        [Tooltip("图标资源路径（用于网络同步加载）")]
        public string iconResourcePath;

        [Tooltip("图标颜色")]
        public Color iconColor = Color.white;

        [Tooltip("是否只触发一次")]
        public bool once = true;

        [Tooltip("是否已触发（网络同步）")]
        public bool isTriggered = false;

        [Tooltip("事件描述")]
        [TextArea(2, 4)]
        public string description;

        /// <summary>
        /// 克隆当前事件数据
        /// </summary>
        public TimeEventData Clone()
        {
            return new TimeEventData
            {
                eventId = this.eventId,
                eventName = this.eventName,
                triggerTime = this.triggerTime,
                limittime = this.limittime,
                eventIcon = this.eventIcon,
                iconResourcePath = this.iconResourcePath,
                iconColor = this.iconColor,
                once = this.once,
                isTriggered = this.isTriggered,
                description = this.description,
                requireCount = this.requireCount,
            };
        }
    }

    /// <summary>
    /// 游戏时间状态（用于网络同步）
    /// </summary>
    public struct GameTimeState
    {
        public float currentTime;
        public bool isRunning;
        public double serverTime; // 服务器时间戳

        public GameTimeState(float time, bool running, double serverTimestamp)
        {
            currentTime = time;
            isRunning = running;
            serverTime = serverTimestamp;
        }
    }
}
