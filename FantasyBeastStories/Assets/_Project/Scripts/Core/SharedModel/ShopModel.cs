using System.Collections.Generic;

namespace Core.SharedModel
{
    /// <summary>
    /// 商店模型 — 纯 C# 类，不继承 MonoBehaviour，可独立单元测试。
    ///
    /// 持有购买记录与库存计算逻辑。
    /// 外部依赖（CoinManager / RuneInventory / AudioManager / ShopDatabase）
    /// 由 Controller 处理，Model 只管理数据与 EventChannel 通知。
    /// </summary>
    public class ShopModel
    {
        /// <summary>购买记录：runeId → 已购买数量</summary>
        private readonly Dictionary<int, int> _purchaseRecords = new();

        /// <summary>获取购买记录（供存档使用）</summary>
        public Dictionary<int, int> GetPurchaseRecords() => _purchaseRecords;

        /// <summary>从存档恢复购买记录</summary>
        public void SetPurchaseRecords(Dictionary<int, int> records)
        {
            _purchaseRecords.Clear();
            if (records == null) return;
            foreach (var kvp in records)
                _purchaseRecords[kvp.Key] = kvp.Value;
        }

        /// <summary>记录一次购买</summary>
        public void RecordPurchase(int runeId)
        {
            if (!_purchaseRecords.ContainsKey(runeId))
                _purchaseRecords[runeId] = 0;
            _purchaseRecords[runeId]++;
        }

        /// <summary>
        /// 查询剩余库存。
        /// 返回 -1 表示不限量商品。
        /// </summary>
        public int GetRemainingStock(int runeId, bool isLimitedStock, int maxStock)
        {
            if (!isLimitedStock) return -1;

            _purchaseRecords.TryGetValue(runeId, out int purchased);
            return System.Math.Max(0, maxStock - purchased);
        }

        /// <summary>
        /// 检查符文是否可购买（库存检查）。
        /// 不检查金币和已拥有状态——那些由 Controller 处理。
        /// </summary>
        public bool IsRuneAvailable(int runeId, bool isLimitedStock, int maxStock)
        {
            if (!isLimitedStock) return true;
            return GetRemainingStock(runeId, isLimitedStock, maxStock) > 0;
        }
    }
}
