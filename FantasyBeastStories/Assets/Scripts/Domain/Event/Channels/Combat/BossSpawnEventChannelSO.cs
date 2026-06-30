using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.Combat
{
    /// <summary>
    /// Boss 生成事件通道
    /// 参数：string bossName — 要生成的 Boss 名称/预制体路径
    /// 由 SyncedGameTimeManager 在时间条件满足时 Raise
    /// 由 BossSpawner 注册监听并执行实际生成逻辑
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Combat/Boss Spawn Event Channel")]
    public class BossSpawnEventChannelSO : BaseEventChannelSO<string> { }
}