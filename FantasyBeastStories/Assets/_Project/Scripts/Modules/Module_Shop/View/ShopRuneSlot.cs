using Controllers.Rune;
using UI.Framework;
using UnityEngine;
using UnityEngine.UI;
using Core;
using Controllers.Shop;

namespace UI.Shop
{
  public class ShopRuneSlot : MonoBehaviour
  {
    [Header("价格显示")]
    [SerializeField] private Text priceText;
    [SerializeField] private Text stockText;

    private RuneSlot runeSlot;
    private ShopRuneConfigSO shopRuneConfig;
    private System.Action<ShopRuneConfigSO, RuneSlot> onSelected;

    public void Initialize(RuneSlot slot, ShopRuneConfigSO config, System.Action<ShopRuneConfigSO, RuneSlot> selectedCallback)
    {
      runeSlot = slot;
      shopRuneConfig = config;
      onSelected = selectedCallback;

      if (runeSlot != null)
      {
        runeSlot.Setup(config.runeData);
        runeSlot.OnClicked += OnSlotClicked;
      }

      UpdatePriceAndStock();
    }

    private void OnSlotClicked(RuneSlot slot)
    {
      onSelected?.Invoke(shopRuneConfig, runeSlot);
    }

    public void Deselect()
    {
      if (runeSlot != null && runeSlot.IsSelected)
        runeSlot.PlayDeselect();
    }

    public void UpdatePriceAndStock()
    {
      if (priceText != null && shopRuneConfig != null)
      {
        priceText.text = $"{shopRuneConfig.price} 金币";
      }

      if (stockText != null && shopRuneConfig != null)
      {
        if (shopRuneConfig.isLimitedStock)
        {
          int remaining = ServiceLocator.Get<ShopManager>().GetRemainingStock(shopRuneConfig.runeId);
          stockText.text = remaining > 0 ? $"库存: {remaining}" : "已售罄";
          stockText.gameObject.SetActive(true);
        }
        else
        {
          stockText.gameObject.SetActive(false);
        }
      }

      var image = GetComponent<Image>();
      if (image == null) return;

      bool isPurchased = shopRuneConfig != null
          && !shopRuneConfig.allowRepeatPurchase
          && RuneInventory.HasRune(shopRuneConfig.runeId);
      float alpha = image.color.a;
      image.color = isPurchased
          ? new Color(0.5f, 0.5f, 0.5f, alpha)
          : new Color(1f, 1f, 1f, alpha);
    }

    void OnDestroy()
    {
      if (runeSlot != null)
      {
        runeSlot.OnClicked -= OnSlotClicked;
      }
    }
  }
}