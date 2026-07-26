using UnityEngine;

namespace Core.Channels.Combat
{
    [CreateAssetMenu(menuName = "Events/SubContainers/Combat Channels")]
    public class CombatChannelsSO : ScriptableObject
    {
        public DamageEventChannelSO damageEventChannel;
        public PlayerDamageEventChannelSO playerDamageEventChannel;
        public EnemyReportEventChannelSO enemyReportChannel;
        public DamageDisplayEventChannelSO damageDisplayChannel;
        public BossHPUpdateEventChannelSO bossHPUpdateChannel;
        public BossDeathEventChannelSO bossDeathChannel;
    }
}