using UnityEngine;
using Domain.Event;
using Domain.Event.Channels.Base;

namespace Domain.Event.Channels.Combat
{
    [CreateAssetMenu(menuName = "Events/Combat/Damage Event Channel")]
    public class DamageEventChannelSO : BaseEventChannelSO<DamageEventArgs> { }
}
