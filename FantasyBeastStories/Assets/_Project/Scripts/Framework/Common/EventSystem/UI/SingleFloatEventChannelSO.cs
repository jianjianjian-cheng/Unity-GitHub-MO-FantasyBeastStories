using UnityEngine;
using Core.Channels.Base;

namespace Core.Channels
{
    [CreateAssetMenu(menuName = "Events/General/Single Float Event Channel")]
    public class SingleFloatEventChannelSO : BaseEventChannelSO<float> { }
}
