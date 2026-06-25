using UnityEngine;
using Domain.Event.Channels.Base;

namespace Domain.Event.Channels
{
    [CreateAssetMenu(menuName = "Events/General/Float Event Channel")]
    public class FloatEventChannelSO : BaseEventChannelSO<float, float> { }
}
