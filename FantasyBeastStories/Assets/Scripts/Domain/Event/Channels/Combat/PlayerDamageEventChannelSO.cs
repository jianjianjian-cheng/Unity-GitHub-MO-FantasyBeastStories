using UnityEngine;
using Domain.Event.Channels.Base;

namespace Domain.Event.Channels.Combat
{
    [CreateAssetMenu(menuName = "Events/Combat/Player Damage Event Channel")]
    public class PlayerDamageEventChannelSO : BaseEventChannelSO<Domain.Event.DamageEventArgs> { }
}
