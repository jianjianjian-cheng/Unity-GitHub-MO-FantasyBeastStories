using Core.Channels.Base;
using UnityEngine;

namespace Core.Channels.Combat
{
    [CreateAssetMenu(menuName = "Events/Combat/Damage Display Event Channel")]
    public class DamageDisplayEventChannelSO : BaseEventChannelSO<DamageDisplayEventArgs> { }
}