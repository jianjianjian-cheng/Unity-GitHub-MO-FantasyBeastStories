using System.Collections.Generic;
using Core;
using Core.SharedModel;
using Controllers.Rune;
using Core.Audio;
using UnityEngine;
using Core.Save;
using Managers;

namespace Managers
{
  /// <summary>
  /// 商店控制器 — 薄层 MonoBehaviour，持有 ShopModel 实例。
  ///
  /// 职责：
  /// - 生命周期管理（单例 + DontDestroyOnLoad）
  /// - 存档注册（ISaveable）
  /// - 处理外部依赖（CoinManager / RuneInventory / AudioManager / ShopDatabase）
  /// - 业务逻辑委托给 ShopModel
  /// </summary>
  public class ShopManager : MonoBehaviour, ISaveable
  {
    private static ShopManager _instance;

    [Header("商店配置")]
    [SerializeField] private ShopRuneDatabaseSO shopDatabase;

    /// <summary>商店模型实例（纯 C#，可单测）</summary>
    public ShopModel Model { get; private set; }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        ServiceLocator.Register(this);
        DontDestroyOnLoad(gameObject);
        Model = new ShopModel();
    }

    void Start()
    {
      ServiceLocator.Get<SaveManager>()?.RegisterSaveable(this);
    }

    void OnDestroy()
    {
            if (_instance == this)
            {
                _instance = null;
                ServiceLocator.Unregister<ShopManager>();
                ServiceLocator.Get<SaveManager>()?.UnregisterSaveable(this);
            }
    }

    // ========== ISaveable 实现 ==========

    public string SaveId => "ShopManager";

    public void OnSave(SaveData data) => data.shopPurchaseRecords = Model.GetPurchaseRecords();
    public void OnLoad(SaveData data) => Model.SetPurchaseRecords(data.shopPurchaseRecords);

    // ========== 购买流程 ==========

    /// <summary>
    /// 购买符文：检查库存 → 检查重复 → 扣金币 → 记录 → 添加符文 → 通知 UI
    /// </summary>
    public bool PurchaseRune(int runeId)
    {
      var config = shopDatabase.GetRuneById(runeId);
      if (config == null)
      {
        Debug.LogError($"[ShopManager] 符文 ID={runeId} 不存在于商店数据库");
        return false;
      }

      // 1. 库存检查（委托 Model）
      if (!Model.IsRuneAvailable(runeId, config.isLimitedStock, config.maxStock))
      {
        Debug.Log($"[ShopManager] 符文 ID={runeId} 已售罄");
        return false;
      }

      // 2. 重复购买检查（依赖 RuneInventory）
      if (!config.allowRepeatPurchase && RuneInventory.HasRune(runeId))
      {
        Debug.Log($"[ShopManager] 符文 ID={runeId} 已拥有，且不允许重复购买");
        return false;
      }

      // 3. 扣金币（依赖 CoinManager）
      if (!ServiceLocator.Get<CoinManager>().SpendCoins(config.price))
      {
        Debug.Log($"[ShopManager] 金币不足，无法购买符文 ID={runeId}");
        return false;
      }

      // 4. 记录购买（委托 Model）
      Model.RecordPurchase(runeId);

      // 5. 添加符文到背包（依赖 RuneInventory）
      RuneInventory.AddRune(runeId);

      // 6. 播放音效
      AudioManager.Instance?.PlayUI("sfx_Coin");

      // 7. 通知 UI 层
      var eventData = new RunePurchasedEventData
      {
        runeId = runeId,
        price = config.price,
        remainingStock = Model.GetRemainingStock(runeId, config.isLimitedStock, config.maxStock)
      };
      EventChannelLocator.MainContainer?.shopEventChannel?.RaiseRunePurchased(eventData);

      Debug.Log($"[ShopManager] 购买成功：符文 ID={runeId}，花费 {config.price} 金币");
      return true;
    }

    // ========== 便捷转发（向后兼容） ==========

    public bool IsRuneAvailable(int runeId)
    {
      var config = shopDatabase.GetRuneById(runeId);
      if (config == null) return false;
      return Model.IsRuneAvailable(runeId, config.isLimitedStock, config.maxStock);
    }

    public int GetRemainingStock(int runeId)
    {
      var config = shopDatabase.GetRuneById(runeId);
      if (config == null) return 0;
      return Model.GetRemainingStock(runeId, config.isLimitedStock, config.maxStock);
    }

    public Dictionary<int, int> GetPurchaseRecords() => Model.GetPurchaseRecords();

    public void SetPurchaseRecords(Dictionary<int, int> records) => Model.SetPurchaseRecords(records);
  }
}
