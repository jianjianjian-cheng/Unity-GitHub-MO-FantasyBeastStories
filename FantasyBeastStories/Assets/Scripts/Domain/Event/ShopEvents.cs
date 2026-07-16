using System;
using Domain.Rune;

namespace Domain.Event
{
    /// <summary>
    /// 符文购买事件数据
    /// </summary>
    public struct RunePurchasedEventData
    {
        public int runeId;          // 购买的符文ID
        public int price;           // 购买价格
        public int remainingStock;  // 剩余库存（如果是限量商品）
    }

    /// <summary>
    /// 商店事件通道接口
    /// </summary>
    public interface IShopEventChannel
    {
        event Action<RunePurchasedEventData> OnRunePurchased;
        event Action OnShopOpened;
        event Action OnShopClosed;

        void RaiseRunePurchased(RunePurchasedEventData data);
        void RaiseShopOpened();
        void RaiseShopClosed();
    }

    /// <summary>
    /// 商店事件通道实现
    /// </summary>
    public class ShopEventChannel : IShopEventChannel
    {
        public event Action<RunePurchasedEventData> OnRunePurchased;
        public event Action OnShopOpened;
        public event Action OnShopClosed;

        public void RaiseRunePurchased(RunePurchasedEventData data)
        {
            OnRunePurchased?.Invoke(data);
        }

        public void RaiseShopOpened()
        {
            OnShopOpened?.Invoke();
        }

        public void RaiseShopClosed()
        {
            OnShopClosed?.Invoke();
        }
    }
}