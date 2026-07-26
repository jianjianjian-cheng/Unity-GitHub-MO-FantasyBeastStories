using UnityEngine;
using Core.Channels.Base;

namespace Core.Channels
{
    [CreateAssetMenu(menuName = "Events/Rune/Rune Info Event Channel")]
    public class RuneInfoEventChannelSO : BaseEventChannelSO<RuneEquipArgs> { }
}
