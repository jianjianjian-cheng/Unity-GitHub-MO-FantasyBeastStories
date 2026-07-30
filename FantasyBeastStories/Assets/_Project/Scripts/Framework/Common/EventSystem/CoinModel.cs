using UnityEngine;

namespace Core.SharedModel
{
    /// <summary>
    /// 金币模型 — 纯 C# 类，不继承 MonoBehaviour，可独立单元测试。
    /// 持有金币数据与业务逻辑，通过 EventChannelSO 通知 View 层。
    /// </summary>
    public class CoinModel
    {
        private readonly int _baseCoinPerKill;
        private readonly float _damageCoinFactor;

        public int CurrentCoins { get; private set; }

        public CoinModel(int baseCoinPerKill, float damageCoinFactor)
        {
            _baseCoinPerKill = baseCoinPerKill;
            _damageCoinFactor = damageCoinFactor;
        }

        public int GetCoins() => CurrentCoins;

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;

            CurrentCoins += amount;
            RaiseCoinUpdate(amount);
        }

        public bool SpendCoins(int amount)
        {
            if (amount <= 0) return false;
            if (CurrentCoins < amount) return false;

            CurrentCoins -= amount;
            RaiseCoinUpdate(-amount);
            return true;
        }

        public void SetCoins(int amount)
        {
            if (amount < 0) amount = 0;

            int delta = amount - CurrentCoins;
            CurrentCoins = amount;
            RaiseCoinUpdate(delta);
        }

        public int CalculateCoins(int kills, int totalDamage)
        {
            int killReward = kills * _baseCoinPerKill;
            int damageReward = Mathf.FloorToInt(totalDamage * _damageCoinFactor);
            return killReward + damageReward;
        }

        public void ResetCoins()
        {
            CurrentCoins = 0;
            RaiseCoinUpdate(0);
        }

        /// <summary>
        /// 广播当前金币数到事件通道，用于 UI 初始化。
        /// </summary>
        public void BroadcastCurrentCoins()
        {
            RaiseCoinUpdate(0);
        }

        private void RaiseCoinUpdate(int delta)
        {
            if (EventChannelLocator.MainContainer?.coinUpdateChannel == null) return;

            var data = new CoinUpdateData(CurrentCoins, delta);
            EventChannelLocator.MainContainer.coinUpdateChannel.Raise(data);
        }
    }
}
