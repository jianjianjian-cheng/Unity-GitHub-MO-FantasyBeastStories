using System.Collections.Generic;
using Core;
using Controllers.Rune;
using UnityEngine;

namespace Managers
{
  public class ShopManager : MonoBehaviour
  {
    public static ShopManager Instance { get; private set; }

    [Header("商店配置")]
    [SerializeField] private ShopRuneDatabaseSO shopDatabase;

    private Dictionary<int, int> purchaseRecords = new Dictionary<int, int>();

    void Awake()
    {
      if (Instance == null)
      {
        Instance = this;
        DontDestroyOnLoad(gameObject);
      }
      else
      {
        Destroy(gameObject);
      }
    }

    public bool PurchaseRune(int runeId)
    {
      var config = shopDatabase.GetRuneById(runeId);
      if (config == null)
      {
        Debug.LogError($"[ShopManager] 符文 ID={runeId} 不存在于商店数据库");
        return false;
      }

      if (!IsRuneAvailable(runeId))
      {
        Debug.Log($"[ShopManager] 符文 ID={runeId} 已售罄");
        return false;
      }

      if (!config.allowRepeatPurchase && RuneInventory.HasRune(runeId))
      {
        Debug.Log($"[ShopManager] 符文 ID={runeId} 已拥有，且不允许重复购买");
        return false;
      }

      if (!CoinManager.Instance.SpendCoins(config.price))
      {
        Debug.Log($"[ShopManager] 金币不足，无法购买符文 ID={runeId}");
        return false;
      }

      RecordPurchase(runeId);
      RuneInventory.AddRune(runeId);

      AudioManager.Instance?.PlayUI("sfx_Coin");

      var eventData = new RunePurchasedEventData
      {
        runeId = runeId,
        price = config.price,
        remainingStock = GetRemainingStock(runeId)
      };
      EventChannelLocator.MainContainer?.shopEventChannel?.RaiseRunePurchased(eventData);

      Debug.Log($"[ShopManager] 购买成功：符文 ID={runeId}，花费 {config.price} 金币");
      return true;
    }

    public bool IsRuneAvailable(int runeId)
    {
      var config = shopDatabase.GetRuneById(runeId);
      if (config == null) return false;

      if (!config.isLimitedStock) return true;

      int remaining = GetRemainingStock(runeId);
      return remaining > 0;
    }

    public int GetRemainingStock(int runeId)
    {
      var config = shopDatabase.GetRuneById(runeId);
      if (config == null) return 0;

      if (!config.isLimitedStock) return -1;

      purchaseRecords.TryGetValue(runeId, out int purchased);
      return Mathf.Max(0, config.maxStock - purchased);
    }

    private void RecordPurchase(int runeId)
    {
      if (!purchaseRecords.ContainsKey(runeId))
      {
        purchaseRecords[runeId] = 0;
      }
      purchaseRecords[runeId]++;
    }

    public Dictionary<int, int> GetPurchaseRecords()
    {
      return purchaseRecords;
    }

    public void SetPurchaseRecords(Dictionary<int, int> records)
    {
      purchaseRecords = new Dictionary<int, int>(records);
    }
  }
}