using UnityEngine;

namespace Domain.Event.Channels.Combat
{
    [CreateAssetMenu(menuName = "Events/SubContainers/Combat Channels")]
    public class CombatChannelsSO : ScriptableObject
    {
        public DamageEventChannelSO damageEventChannel;
        public PlayerDamageEventChannelSO playerDamageEventChannel;
        public EnemyReportEventChannelSO enemyReportChannel;
        public DamageDisplayEventChannelSO damageDisplayChannel;
        public BossHPUpdateEventChannelSO bossHPUpdateChannel;
    }
}