using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.Game
{
    public class DifficultyCoefficientQueryData : EventArgsBase
    {
        public float result;
    }

    [CreateAssetMenu(menuName = "Events/Game/Difficulty Coefficient Query Event Channel")]
    public class DifficultyCoefficientQueryEventChannelSO : BaseEventChannelSO<DifficultyCoefficientQueryData>
    {
    }
}
