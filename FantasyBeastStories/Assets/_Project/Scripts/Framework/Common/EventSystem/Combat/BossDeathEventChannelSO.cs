using Core.Channels.Base;
using UnityEngine;

namespace Core.Channels.Combat
{
    /// <summary>
    /// Boss 死亡事件通道（无参数）
    /// 由 Boss_Horror.DeathSequence() 在死亡时 Raise
    /// 由 GameManager 监听并触发返回大厅的逻辑
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Combat/Boss Death Event Channel")]
    public class BossDeathEventChannelSO : BaseEventChannelSO { }
}