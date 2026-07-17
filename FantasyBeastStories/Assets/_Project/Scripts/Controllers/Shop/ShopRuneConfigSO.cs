using System.Collections.Generic;
using Controllers.Rune;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Shop Rune Config")]
public class ShopRuneConfigSO : ScriptableObject
{
  [Header("基础符文数据")]
  public RuneDataSO runeData;

  [Header("商店配置")]
  public int price;
  public bool isLimitedStock;
  public int maxStock = 1;
  [Tooltip("勾选后允许重复购买（已拥有也可再次购买）")]
  public bool allowRepeatPurchase;

  public int runeId => runeData?.runeId ?? -1;
  public string runeName => runeData?.runeName ?? string.Empty;
  public Sprite icon => runeData?.icon;
  public Rarity quality => runeData?.rarity ?? Rarity.Common;
  public string description
  {
    get
    {
      if (runeData == null) return string.Empty;
      if (!string.IsNullOrEmpty(runeData.specialPowerDescription))
        return runeData.specialPowerDescription;

      if (runeData.powers == null || runeData.powers.Count == 0)
        return string.Empty;

      var parts = new List<string>();
      foreach (var power in runeData.powers)
      {
        parts.Add($"{(power.value > 0 ? "+" : "")}{power.value}{power.label}");
      }

      return string.Join("\n", parts);
    }
  }
}