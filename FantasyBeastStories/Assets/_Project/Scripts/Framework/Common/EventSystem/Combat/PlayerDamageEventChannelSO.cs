using UnityEngine;
using Core.Channels.Base;

namespace Core.Channels.Combat
{
    [CreateAssetMenu(menuName = "Events/Combat/Player Damage Event Channel")]
    public class PlayerDamageEventChannelSO : BaseEventChannelSO<DamageEventArgs> { }
}
