using UnityEngine;
using Domain.Event.Channels.Base;

namespace Domain.Event.Channels
{
    [CreateAssetMenu(menuName = "Events/General/Single Float Event Channel")]
    public class SingleFloatEventChannelSO : BaseEventChannelSO<float> { }
}
