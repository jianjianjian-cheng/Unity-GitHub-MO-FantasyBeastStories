using System;
using UnityEngine;

namespace Core.EventSystem
{
    /// <summary>
    /// 事件参数基类
    /// </summary>
    public abstract class GameEventArgs : EventArgs
    {
        /// <summary>
        /// 事件发生的时间戳
        /// </summary>
        public float Timestamp { get; set; } = Time.time;
    }
}
