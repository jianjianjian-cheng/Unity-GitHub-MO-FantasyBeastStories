using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.Player
{
    [CreateAssetMenu(menuName = "Events/Player/Experience Event Channel")]
    public class ExperienceEventChannelSO : BaseEventChannelSO<int>
    {
    }
}
