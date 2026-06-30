namespace Domain.Event
{
    /// <summary>
    /// 金币更新数据，用于 CoinManager → UI 层通信
    /// </summary>
    public class CoinUpdateData : EventArgsBase
    {
        /// <summary>当前金币总数</summary>
        public int CurrentCoins { get; set; }

        /// <summary>本次变化量（正数为增加，负数为减少）</summary>
        public int Delta { get; set; }

        public CoinUpdateData(int currentCoins, int delta = 0)
        {
            CurrentCoins = currentCoins;
            Delta = delta;
        }
    }
}