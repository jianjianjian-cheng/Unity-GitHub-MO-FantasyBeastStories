using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.Combat
{
    /// <summary>
    /// Boss 血量更新事件数据
    /// </summary>
    public class BossHPUpdateData : EventArgsBase
    {
        public float maxHealth;
        public float currentHealth;
        public string bossName;
        /// <summary>
        /// true = 初始化显示BossUI（含名称）/ false = 仅更新血量
        /// </summary>
        public bool isInitialized;
    }

    [CreateAssetMenu(menuName = "Events/Combat/Boss HP Update Channel")]
    public class BossHPUpdateEventChannelSO : BaseEventChannelSO<BossHPUpdateData>
    {
    }
}