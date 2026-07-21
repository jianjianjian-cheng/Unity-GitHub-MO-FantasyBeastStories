using System.Collections.Generic;
using Core;
using Controllers.Rune;
using UI.Framework;
using UI.Framework.Base;
using UI.Framework.Manager;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI.Framework.Animation;

public class ShopPanel : UIScreen
{
  [Header("商店符文列表（与符文面板一致）")]
  [SerializeField] private GameObject runeSlotPrefab;
  [SerializeField] private Transform runeSlotContainer;

  [Header("详情信息（只显示名字、描述、价格）")]
  [SerializeField] private GameObject detailPanel;
  [SerializeField] private TextMeshProUGUI detailName;
  [SerializeField] private TextMeshProUGUI detailDescription;
  [SerializeField] private TextMeshProUGUI detailPrice;
  [SerializeField] private Button purchaseButton;

  [Header("金币显示")]
  [SerializeField] private TextMeshProUGUI coinText;

  [SerializeField] private ShopRuneDatabaseSO shopDatabase;

  private List<GameObject> shopRuneSlots = new List<GameObject>();
  private ShopRuneConfigSO selectedRune;
  private RuneSlot selectedRuneSlot;
  private bool isListenerInitialized;

  protected override void Awake()
  {
    screenId = "ShopPanel";
    base.Awake();
    UIManager.Instance.RegisterScreen(this);
  }

  protected override void Start()
  {
    base.Start();
    ResolveDetailReferences();
    InitializeShop();
  }

  protected override void OnBeforeOpen()
  {
    base.OnBeforeOpen();
    EnsureListenerInitialized();
    UpdateCoinDisplay();
    RefreshShopItems();
  }

  protected override void OnAfterClose()
  {
    base.OnAfterClose();
    ClearSelection();
  }

  private void ResolveDetailReferences()
  {
    if (detailName == null)
      detailName = FindChildTMP("DetailName");
    if (detailDescription == null)
      detailDescription = FindChildTMP("DetailDescription");
    if (detailPrice == null)
      detailPrice = FindChildTMP("DetailPrice");
    if (purchaseButton == null)
      purchaseButton = GetComponentInChildren<Button>(true);
  }

  private TextMeshProUGUI FindChildTMP(string name)
  {
    var child = transform.Find(name);
    return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
  }

  private void EnsureListenerInitialized()
  {
    if (isListenerInitialized) return;
    isListenerInitialized = true;

    if (purchaseButton != null)
    {
      purchaseButton.onClick.AddListener(OnPurchaseClicked);
      if (purchaseButton.GetComponent<ButtonFloatAnim>() == null)
        purchaseButton.gameObject.AddComponent<ButtonFloatAnim>();
    }

    var coinChannel = EventChannelLocator.MainContainer?.coinUpdateChannel;
    if (coinChannel != null)
    {
      coinChannel.OnEventRaised += OnCoinUpdated;
    }
  }

  private void InitializeShop()
  {
    EnsureListenerInitialized();

    if (detailPanel != null)
    {
      detailPanel.SetActive(false);
    }
  }

  private void OnCoinUpdated(CoinUpdateData data)
  {
    UpdateCoinDisplay();
  }

  private void RefreshShopItems()
  {
    // 刷新列表时旧 slot 会被销毁，清空选中引用
    selectedRuneSlot = null;

    foreach (var slot in shopRuneSlots)
    {
      Destroy(slot);
    }
    shopRuneSlots.Clear();

    if (shopDatabase == null)
    {
      Debug.LogError("[ShopPanel] shopDatabase 未赋值");
      return;
    }

    if (runeSlotPrefab == null || runeSlotContainer == null)
    {
      Debug.LogError("[ShopPanel] runeSlotPrefab 或 runeSlotContainer 未赋值");
      return;
    }

    foreach (var runeConfig in shopDatabase.shopRunes)
    {
      GameObject slotObj = Instantiate(runeSlotPrefab, runeSlotContainer);
      ShopRuneSlot shopSlot = slotObj.AddComponent<ShopRuneSlot>();

      RuneSlot runeSlot = slotObj.GetComponent<RuneSlot>();
      if (runeSlot != null)
      {
        shopSlot.Initialize(runeSlot, runeConfig, OnRuneSelected);
      }

      shopRuneSlots.Add(slotObj);
    }
  }

  private void UpdateCoinDisplay()
  {
    if (coinText != null)
    {
      int coins = Managers.CoinManager.Instance.GetCoins();
      coinText.text = $"金币: {FormatCoins(coins)}";
    }
  }

  private static string FormatCoins(int amount)
  {
    if (amount < 1000)
      return amount.ToString();
    if (amount < 1000000)
      return (amount / 1000f).ToString("0.#") + "K";
    return (amount / 1000000f).ToString("0.#") + "M";
  }

  private void OnRuneSelected(ShopRuneConfigSO runeConfig, RuneSlot runeSlot)
  {
    // 取消上一个选中项的背景图
    if (selectedRuneSlot != null && selectedRuneSlot != runeSlot)
    {
      selectedRuneSlot.PlayDeselect();
    }

    // 选中当前项，切换背景图
    selectedRuneSlot = runeSlot;
    if (runeSlot != null && !runeSlot.IsSelected)
    {
      runeSlot.PlaySelect();
    }

    selectedRune = runeConfig;

    // 显示详情面板
    if (detailPanel != null)
    {
      detailPanel.SetActive(true);
    }

    // 更新详情文本（不依赖 detailPanel 是否赋值）
    if (detailName != null)
      detailName.text = runeConfig.runeName;
    if (detailDescription != null)
    {
      var effectivePowers = RuneEffectApplier.GetEffectivePowers(runeConfig.runeId);
      var parts = new System.Text.StringBuilder();
      foreach (var power in effectivePowers)
        parts.AppendLine($"{(power.value > 0 ? "+" : "")}{power.value}{power.label}");
      var specialDesc = RuneEffectApplier.GetEffectiveSpecialPowerDescription(runeConfig.runeId);
      if (!string.IsNullOrEmpty(specialDesc))
        parts.Append(specialDesc);
      detailDescription.text = parts.ToString().TrimEnd();
    }
    if (detailPrice != null)
      detailPrice.text = $"{runeConfig.price} ";

    UpdatePurchaseButtonState();

    // 使用RuneInfoChannel展示详情信息，与RunePanel保持一致
    if (runeConfig.runeData != null)
    {
      var runePowers = RuneEffectApplier.GetEffectivePowers(runeConfig.runeId);

      var args = new Core.RuneEquipArgs(
          runeConfig.runeId,
          runeConfig.runeData.runeName,
          runePowers,
          RuneEffectApplier.GetEffectiveSpecialPowerName(runeConfig.runeId),
          RuneEffectApplier.GetEffectiveSpecialPowerDescription(runeConfig.runeId)
      );
      EventChannelLocator.MainContainer.runeInfoChannel.Raise(args);
    }
  }

  private void OnPurchaseClicked()
  {
    if (selectedRune == null)
    {
      Debug.LogWarning("[ShopPanel] 购买失败：未选中符文");
      return;
    }

    Debug.Log($"[ShopPanel] 点击购买，runeId={selectedRune.runeId}, price={selectedRune.price}, allowRepeat={selectedRune.allowRepeatPurchase}");
    bool success = Managers.ShopManager.Instance.PurchaseRune(selectedRune.runeId);
    Debug.Log($"[ShopPanel] 购买结果: {success}");

    if (success)
    {
      RefreshShopItems();
      UpdatePurchaseButtonState();
    }
  }

  private void UpdatePurchaseButtonState()
  {
    if (purchaseButton == null || selectedRune == null) return;

    bool isAvailable = Managers.ShopManager.Instance.IsRuneAvailable(selectedRune.runeId);
    bool hasPurchased = !selectedRune.allowRepeatPurchase && RuneInventory.HasRune(selectedRune.runeId);
    int currentCoins = Managers.CoinManager.Instance.GetCoins();
    bool canAfford = currentCoins >= selectedRune.price;

    purchaseButton.interactable = isAvailable && !hasPurchased && canAfford;

    var btnText = purchaseButton.GetComponentInChildren<TextMeshProUGUI>();
    if (btnText != null)
    {
      btnText.text = hasPurchased ? "已购买" : (purchaseButton.interactable ? "购买" : "不可购买");
    }
  }

  private void ClearSelection()
  {
    if (selectedRuneSlot != null && selectedRuneSlot.IsSelected)
    {
      selectedRuneSlot.PlayDeselect();
    }
    selectedRuneSlot = null;
    selectedRune = null;

    if (detailPanel != null)
    {
      detailPanel.SetActive(false);
    }
  }

  void OnDestroy()
  {
    var coinChannel = EventChannelLocator.MainContainer?.coinUpdateChannel;
    if (coinChannel != null)
    {
      coinChannel.OnEventRaised -= OnCoinUpdated;
    }
  }
}
