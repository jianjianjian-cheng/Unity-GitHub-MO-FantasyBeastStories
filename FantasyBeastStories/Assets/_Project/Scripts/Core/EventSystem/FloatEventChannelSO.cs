using UnityEngine;
using Core.Channels.Base;

namespace Core.Channels
{
    [CreateAssetMenu(menuName = "Events/General/Float Event Channel")]
    public class FloatEventChannelSO : BaseEventChannelSO<float, float> { }
}
