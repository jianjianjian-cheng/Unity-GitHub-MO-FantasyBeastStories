using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.UI
{
    /// <summary>
    /// 金币更新事件通道
    /// Application 层发送金币变化数据，Presentation 层监听并更新 UI
    /// </summary>
    [CreateAssetMenu(menuName = "Events/UI/Coin Update Event Channel")]
    public class CoinUpdateEventChannelSO : BaseEventChannelSO<CoinUpdateData>
    {
    }
}