using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.UI
{
    /// <summary>
    /// 对局统计数据更新事件通道
    /// Application 层发送对局统计变化数据，Presentation 层监听并更新 UI
    /// </summary>
    [CreateAssetMenu(menuName = "Events/UI/Match Stats Update Event Channel")]
    public class MatchStatsUpdateEventChannelSO : BaseEventChannelSO<MatchStatsUpdateData>
    {
    }
}