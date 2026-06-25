using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.Player
{
    [CreateAssetMenu(menuName = "Events/Player/Player HP Event Channel")]
    public class PlayerHPEventChannelSO : BaseEventChannelSO<PlayerHPData>
    {
    }

    public class PlayerHPData : EventArgsBase
    {
        public string playerId;
        public float currentHP;
        public float maxHP;

        public PlayerHPData(string playerId, float currentHP, float maxHP)
        {
            this.playerId = playerId;
            this.currentHP = currentHP;
            this.maxHP = maxHP;
        }
    }
}
