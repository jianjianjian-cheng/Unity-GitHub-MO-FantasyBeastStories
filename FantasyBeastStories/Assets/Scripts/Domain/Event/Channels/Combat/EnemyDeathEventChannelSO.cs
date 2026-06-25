using UnityEngine;
using Domain.Event.Channels.Base;

namespace Domain.Event.Channels.Combat
{
    [CreateAssetMenu(menuName = "Events/Combat/Enemy Death Event Channel")]
    public class EnemyDeathEventChannelSO : BaseEventChannelSO<GameObject> { }
}
