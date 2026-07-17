using Core.Channels.Base;
using UnityEngine;

namespace Core.Channels.Game
{
    /// <summary>
    /// 道具拾取事件通道
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Game/Power Up Collect Event Channel")]
    public class PowerUpCollectEventChannelSO : BaseEventChannelSO<PowerUpCollectEventData>
    {
    }
}