using Core.Channels.Base;
using UnityEngine;

namespace Core.Channels.Game
{
    /// <summary>
    /// 时间查询数据（请求-响应模式）
    /// 查询方创建实例后调用 Query()，SyncedGameTimeManager 的监听器回填数据
    /// </summary>
    public class TimeQueryData : EventArgsBase
    {
        public float currentTime;
        public float normalizedTime;
        public float totalGameTime;
        public float remainingTime;
        public bool isTimeRunning;
    }

    [CreateAssetMenu(menuName = "Events/Game/Time Query Event Channel")]
    public class TimeQueryEventChannelSO : BaseEventChannelSO<TimeQueryData>
    {
        /// <summary>
        /// 发送查询请求，SyncedGameTimeManager 监听此事件并回填数据
        /// </summary>
        public void Query(TimeQueryData data)
        {
            Raise(data);
        }
    }
}