using UnityEngine;
using Core;
using Core.Channels.Base;

namespace Core.Channels.Combat
{
    [CreateAssetMenu(menuName = "Events/Combat/Damage Event Channel")]
    public class DamageEventChannelSO : BaseEventChannelSO<DamageEventArgs> { }
}
