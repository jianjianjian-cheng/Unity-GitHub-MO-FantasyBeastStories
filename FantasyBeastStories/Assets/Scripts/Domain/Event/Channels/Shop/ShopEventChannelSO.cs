using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.Shop
{
    [CreateAssetMenu(menuName = "Events/Shop/Rune Purchased Event Channel")]
    public class ShopEventChannelSO : BaseEventChannelSO<RunePurchasedEventData>
    {
        public void RaiseRunePurchased(RunePurchasedEventData data) => Raise(data);
    }
}