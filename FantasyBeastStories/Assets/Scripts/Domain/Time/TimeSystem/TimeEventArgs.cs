using Domain.Event;

namespace Domain.Time.TimeSystem
{
    /// <summary>
    /// 时间事件参数
    /// </summary>
    public class TimeEventArgs : EventArgsBase
    {
        /// <summary>
        /// 触发的事件数据
        /// </summary>
        public TimeEventData eventData;

        /// <summary>
        /// 触发时的当前时间
        /// </summary>
        public float currentTime;

        /// <summary>
        /// 是否由网络同步触发
        /// </summary>
        public bool isFromNetwork;

        public TimeEventArgs()
        {
            isFromNetwork = false;
        }

        public TimeEventArgs(TimeEventData eventData, float currentTime, bool isFromNetwork = false)
        {
            this.eventData = eventData;
            this.currentTime = currentTime;
            this.isFromNetwork = isFromNetwork;
        }
    }
}