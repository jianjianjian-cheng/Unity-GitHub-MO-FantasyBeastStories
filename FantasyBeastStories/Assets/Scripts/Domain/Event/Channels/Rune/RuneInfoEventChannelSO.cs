using UnityEngine;
using Domain.Event.Channels.Base;

namespace Domain.Event.Channels
{
    [CreateAssetMenu(menuName = "Events/Rune/Rune Info Event Channel")]
    public class RuneInfoEventChannelSO : BaseEventChannelSO<Domain.Event.RuneEquipArgs> { }
}
