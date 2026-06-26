using UnityEngine;
using Domain.Event.Channels.Base;

namespace Domain.Event.Channels.Game
{
    /// <summary>
    /// 游戏暂停状态广播事件通道
    /// Application 层在暂停/恢复时广播，各子系统监听后自行处理冻结逻辑
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Game/Pause State Event Channel")]
    public class GamePauseStateEventChannelSO : BaseEventChannelSO<bool>
    {
    }
}