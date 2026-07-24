using Core.Channels.Base;
using UnityEngine;

namespace Core.Channels.Shop
{
    [CreateAssetMenu(menuName = "Events/Shop/Rune Purchased Event Channel")]
    public class ShopEventChannelSO : BaseEventChannelSO<RunePurchasedEventData>
    {
        public void RaiseRunePurchased(RunePurchasedEventData data) => Raise(data);
    }
}