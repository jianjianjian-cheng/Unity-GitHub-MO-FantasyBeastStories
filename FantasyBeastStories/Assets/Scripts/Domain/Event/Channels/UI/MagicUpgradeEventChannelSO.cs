using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.UI
{
    [CreateAssetMenu(menuName = "Events/UI/Magic Upgrade Event Channel")]
    public class MagicUpgradeEventChannelSO : BaseEventChannelSO<bool>
    {
    }
}
