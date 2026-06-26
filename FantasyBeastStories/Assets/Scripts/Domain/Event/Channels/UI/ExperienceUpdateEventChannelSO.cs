using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.UI
{
    /// <summary>
    /// 经验值/等级更新事件通道
    /// Application 层发送经验/等级变化数据，Presentation 层监听并更新 UI
    /// </summary>
    [CreateAssetMenu(menuName = "Events/UI/Experience Update Event Channel")]
    public class ExperienceUpdateEventChannelSO : BaseEventChannelSO<ExperienceUpdateData>
    {
    }
}