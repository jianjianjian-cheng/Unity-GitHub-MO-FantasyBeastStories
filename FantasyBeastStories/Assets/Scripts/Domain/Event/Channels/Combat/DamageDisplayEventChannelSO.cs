using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.Combat
{
    [CreateAssetMenu(menuName = "Events/Combat/Damage Display Event Channel")]
    public class DamageDisplayEventChannelSO : BaseEventChannelSO<DamageDisplayEventArgs> { }
}