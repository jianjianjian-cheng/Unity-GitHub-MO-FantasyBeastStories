using System.Collections.Generic;
using UnityEngine;

namespace Controllers.Shop
{
  [CreateAssetMenu(menuName = "Shop/Shop Rune Database")]
  public class ShopRuneDatabaseSO : ScriptableObject
  {
      public List<ShopRuneConfigSO> shopRunes;

      public ShopRuneConfigSO GetRuneById(int runeId)
      {
          return shopRunes.Find(r => r.runeId == runeId);
      }
  }
}
