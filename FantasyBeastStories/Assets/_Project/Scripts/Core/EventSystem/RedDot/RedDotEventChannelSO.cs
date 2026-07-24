using Core.Channels.Base;
using UnityEngine;

namespace Core.Channels.RedDot
{
    /// <summary>
    /// 红点状态变更事件通道。Controller 广播 → View 监听。
    /// </summary>
    [CreateAssetMenu(menuName = "Events/RedDot/RedDot Event Channel")]
    public class RedDotEventChannelSO : BaseEventChannelSO<RedDotEventData> { }
}
