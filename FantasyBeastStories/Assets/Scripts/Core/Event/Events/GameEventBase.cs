using System;

namespace Core.Event
{
    /// <summary>
    /// 游戏事件基类 - 所有游戏事件继承此类
    /// </summary>
    [Serializable]
    public abstract class GameEventBase
    {
        public DateTime Timestamp { get; } = DateTime.Now;
        public string EventName => GetType().Name;
    }
}
