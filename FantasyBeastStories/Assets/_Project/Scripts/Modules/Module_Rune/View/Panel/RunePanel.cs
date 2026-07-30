using System;
using System.Collections.Generic;
using Core;
using Core.Channels.General;
using Controllers.Rune;
using UI.Framework;
using UI.Framework.Base;
using UI.Framework.Manager;
using UnityEngine;
using UnityEngine.UI;
using Core.Audio;
using Controllers.Game;

namespace UI.Rune
{
  public class RunePanel : UIScreen
  {
    [Header("Rune Panel Buttons")]
    [SerializeField] private Button equipButton;
    [SerializeField] private Button breakdownButton;

    [Header("符文列表（动态生成）")]
    [SerializeField] private GameObject runeSlotPrefab;       // 符文插槽预制体
    [SerializeField] private GameObject runeSlotListPanel;    // 符文列表父级 Panel（生成的预制体挂在此 Panel 下）
    private RuneDatabaseSO runeDatabase;                     // 符文数据库（运行时通过 Addressables 加载，确保热更生效）

    [Header("装备插槽")]
    [SerializeField] private RuneEquip runeEquip1;
    [SerializeField] private RuneEquip runeEquip2;

    [Header("拖拽")]
    [SerializeField] private Color dragTargetHighlightColor = new Color(1f, 0.92f, 0.46f, 1f); // 拖拽时装备插槽高亮色

    // 运行时动态生成的符文插槽列表
    private List<GameObject> runeSlotList = new List<GameObject>();

    // 选中状态
    private GameObject selectedRuneListItem;
    private RuneEquip selectedEquip; // 当前选中的装备插槽

    // ── 玩家拥有的符文（后续从存档加载） ──
    private List<int> ownedRuneIds = new List<int>();

    // 默认初始符文
    private static List<int> DefaultRuneIds = new List<int> { 0, 0, 1, 1, 1 };

    /// <summary>玩家已拥有的符文 ID 列表</summary>
    public IReadOnlyList<int> OwnedRuneIds => ownedRuneIds;

    // --- 事件：通知 LobbyCanvas 更新图标视觉 ---
    /// <summary>符文装备事件 (slotIndex, equippedSprite)</summary>
    public event Action<int, Sprite> OnRuneEquipped;
    /// <summary>符文卸下事件 (slotIndex)</summary>
    public event Action<int> OnRuneUnequipped;
    /// <summary>符文列表项选中事件</summary>
    public event Action<RuneSlot> OnRuneItemSelected;

    // ──────────────────────────────────────────────
    //  UIScreen 生命周期
    // ──────────────────────────────────────────────

    protected override void Awake()
    {
      screenId = "RunePanel";
      base.Awake();
      UIManager.Instance.RegisterScreen(this);
      Initialize();
    }

    private void Initialize()
    {
      // 通过 Addressables 加载 RuneDatabase（确保使用热更后的数据）
      runeDatabase = AssetLoader.TryLoadAsset<RuneDatabaseSO>("Lobby_RuneData_RuneDatabase");
      if (runeDatabase != null)
      {
        Debug.Log($"[RunePanel] 通过 Addressables 加载 RuneDatabase 成功，符文数: {runeDatabase.allRunes.Count}");
        foreach (var rune in runeDatabase.allRunes)
        {
          string powers = string.Join(", ", rune.powers.ConvertAll(p => $"{p.label}={p.value}"));
          Debug.Log($"[RunePanel]   ID={rune.runeId} {rune.runeName} | powers: {powers} | special: {rune.specialPowerName}");
        }
      }
      else
      {
        Debug.LogError("[RunePanel] 通过 Addressables 加载 RuneDatabase 失败！");
      }

      // 初始化已拥有符文（默认符文 + RuneInventory 中已购买的符文）
      ownedRuneIds = new List<int>(DefaultRuneIds);
      MergeInventoryRunes();

      // 绑定装备按钮
      if (equipButton != null)
        equipButton.onClick.AddListener(OnEquipClicked);

      // 绑定分解按钮
      if (breakdownButton != null)
        breakdownButton.onClick.AddListener(OnBreakdownClicked);

      // 动态生成符文列表（根据 ownedRuneIds 过滤 RuneDatabase）
      BuildRuneSlotList();

      // 默认装备：RuneEquip_1 ← RuneSlot[0], RuneEquip_2 ← RuneSlot[1]
      EquipDefaultRunes();

      // 刷新已装备标记 + 已装备符文排到最前面
      RefreshAllEquippedMarks();
      ReorderEquippedToFront();

      // 订阅装备插槽选中事件，默认选中插槽1
      if (runeEquip1 != null)
      {
        runeEquip1.OnSelected += OnEquipSlotSelected;
        runeEquip1.OnDropReceived += OnRuneDropReceived;      // 拖拽丢落
        selectedEquip = runeEquip1;
        runeEquip1.SetSelected(true);
      }
      if (runeEquip2 != null)
      {
        runeEquip2.OnSelected += OnEquipSlotSelected;
        runeEquip2.OnDropReceived += OnRuneDropReceived;      // 拖拽丢落
      }

      // 订阅符文拖拽事件（全局，用于高亮可拖放区域）
      RuneSlot.OnDragStarted += OnAnyRuneDragStarted;
      RuneSlot.OnDragEnded += OnAnyRuneDragEnded;
    }

    protected override void OnBeforeOpen()
    {
      base.OnBeforeOpen();

      // 检查是否有新购买的符文，如有则重建列表
      // 或首次加载失败（Addressables 未就绪）时重试
      if (MergeInventoryRunes() || runeSlotList.Count == 0)
      {
        BuildRuneSlotList();
        RestoreEquipReferences();
      }

      // 刷新已装备标记 + 已装备符文排到最前面
      RefreshAllEquippedMarks();
      ReorderEquippedToFront();

      // 取消旧的选中，重新选中列表第 1 项
      DeselectAllItems();
      if (runeSlotList.Count > 0)
        SetRuneSlotItemSelected(runeSlotList[0]);

      // 默认选中装备槽 1
      if (selectedEquip != runeEquip1)
      {
        selectedEquip?.SetSelected(false);
        selectedEquip = runeEquip1;
      }
      if (runeEquip1 != null)
        runeEquip1.SetSelected(true);
    }

    protected override void OnAfterClose()
    {
      base.OnAfterClose();
      DeselectAllItems();
    }

    // ──────────────────────────────────────────────
    //  RuneInventory 同步
    // ──────────────────────────────────────────────

    /// <summary>
    /// 从默认符文 + RuneInventory 已购买符文重建 ownedRuneIds。
    /// 返回 true 表示列表有变化（有新购买的符文）。
    /// </summary>
    private bool MergeInventoryRunes()
    {
      var newIds = new List<int>(DefaultRuneIds);
      newIds.AddRange(RuneInventory.GetAllRuneIds());

      if (newIds.Count == ownedRuneIds.Count)
      {
        bool same = true;
        for (int i = 0; i < newIds.Count; i++)
        {
          if (newIds[i] != ownedRuneIds[i]) { same = false; break; }
        }
        if (same) return false;
      }

      ownedRuneIds = newIds;
      Debug.Log($"[RunePanel] 符文列表更新：共 {ownedRuneIds.Count} 个（默认 {DefaultRuneIds.Count} + 购买 {newIds.Count - DefaultRuneIds.Count}）");
      return true;
    }

    /// <summary>
    /// 重建列表后，重新关联 RuneEquip 的 RuneSlot 引用
    /// （旧引用已随列表销毁，需要指向新的 RuneSlot 实例）
    /// </summary>
    private void RestoreEquipReferences()
    {
      // 标记是否已重新关联了每个装备插槽
      bool equip1Linked = false, equip2Linked = false;

      foreach (var go in runeSlotList)
      {
        if (go == null) continue;
        var slot = go.GetComponent<RuneSlot>();
        if (slot == null || slot.RuneData == null) continue;

        int runeId = slot.RuneData.runeId;

        if (!equip1Linked && runeEquip1 != null && runeEquip1.EquippedRuneId == runeId)
        {
          runeEquip1.Equip(runeId, slot.RuneData.icon, slot);
          equip1Linked = true;
        }
        else if (!equip2Linked && runeEquip2 != null && runeEquip2.EquippedRuneId == runeId)
        {
          runeEquip2.Equip(runeId, slot.RuneData.icon, slot);
          equip2Linked = true;
        }
      }
    }

    // ──────────────────────────────────────────────
    //  动态生成符文插槽
    // ──────────────────────────────────────────────

    private void BuildRuneSlotList()
    {
      // 清空上一次生成的列表
      foreach (var go in runeSlotList)
        Destroy(go);
      runeSlotList.Clear();

      if (runeSlotPrefab == null)
      {
        Debug.LogError("[RunePanel] runeSlotPrefab 未赋值，无法动态生成符文列表");
        return;
      }

      if (runeSlotListPanel == null)
      {
        Debug.LogError("[RunePanel] runeSlotListPanel 未赋值，无法动态生成符文列表");
        return;
      }

      // 使用直接引用的符文数据库（与 ShopPanel 方式一致）
      if (runeDatabase == null || runeDatabase.allRunes.Count == 0)
      {
        Debug.LogWarning("[RunePanel] runeDatabase 未赋值或数据为空");
        return;
      }

      // 构建 runeId → RuneDataSO 字典
      var runeDataMap = new Dictionary<int, RuneDataSO>();
      foreach (var data in runeDatabase.allRunes)
        if (data != null && !runeDataMap.ContainsKey(data.runeId))
          runeDataMap[data.runeId] = data;

      // 对 ownedRuneIds 排序（自动按 runeId 升序排列）
      ownedRuneIds.Sort();

      // 复制新增计数，遍历时逐个消费
      var remainingNew = new Dictionary<int, int>();
      foreach (var kvp in ownedRuneIds)
      {
        int n = RuneInventory.GetNewCount(kvp);
        if (n > 0)
          remainingNew[kvp] = n;
      }

      // 每个 ID 生成一个插槽（支持重复符文）
      for (int i = 0; i < ownedRuneIds.Count; i++)
      {
        int runeId = ownedRuneIds[i];

        if (!runeDataMap.TryGetValue(runeId, out var runeData))
        {
          Debug.LogWarning($"[RunePanel] ownedRuneIds 包含未在数据库中注册的 ID: {runeId}");
          continue;
        }

        // 实例化预制体
        var go = Instantiate(runeSlotPrefab, runeSlotListPanel.transform);
        go.name = $"RuneSlot_{runeId}_{runeData.runeName}_{i:D2}";

        // 设置符文数据
        var runeSlot = go.GetComponent<RuneSlot>();
        if (runeSlot != null)
        {
          runeSlot.Setup(runeData);
          runeSlot.OnClicked += OnRuneSlotClicked;

          // 标记已装备状态
          bool isEquipped = (runeEquip1 != null && runeEquip1.EquippedRuneId == runeId) ||
                            (runeEquip2 != null && runeEquip2.EquippedRuneId == runeId);
          runeSlot.SetEquipped(isEquipped);

          // 仅给新增名额分配红点（同一 runeId 只点亮前 N 个，N=新增数量）
          if (remainingNew.TryGetValue(runeId, out var remaining) && remaining > 0)
          {
            runeSlot.SetRedDotVisible(true);
            remainingNew[runeId] = remaining - 1;
          }
        }

        runeSlotList.Add(go);
      }
    }

    private void OnRuneSlotClicked(RuneSlot runeSlot)
    {
      if (runeSlot == null || runeSlot.gameObject == selectedRuneListItem)
        return;

      SetRuneSlotItemSelected(runeSlot.gameObject);
    }

    /// <summary>
    /// 默认装备：取前两个不同种类的符文分别装备到两个插槽
    /// </summary>
    private void EquipDefaultRunes()
    {
      var usedRuneIds = new HashSet<int>();
      int equipIndex = 0;
      RuneEquip[] equipSlots = { runeEquip1, runeEquip2 };

      for (int i = 0; i < runeSlotList.Count && equipIndex < 2; i++)
      {
        var slot = runeSlotList[i].GetComponent<RuneSlot>();
        if (slot == null || slot.RuneData == null)
          continue;

        int runeId = slot.RuneData.runeId;

        // 跳过已装备过的符文种类
        if (usedRuneIds.Contains(runeId))
          continue;

        usedRuneIds.Add(runeId);
        var equip = equipSlots[equipIndex];
        if (equip != null)
        {
          equip.Equip(runeId, slot.RuneData.icon, slot);
          equipIndex++;
        }
      }

      if (equipIndex == 0)
        Debug.LogWarning("[RunePanel] 没有可装备的默认符文");
    }

    private void SetRuneSlotItemSelected(GameObject item)
    {
      // 取消选中之前的项
      if (selectedRuneListItem != null && selectedRuneListItem != item)
      {
        var prevSlot = selectedRuneListItem.GetComponent<RuneSlot>();
        prevSlot?.ForceDeselect();
      }

      selectedRuneListItem = item;

      // 选中新项 + 显示详情
      var runeSlot = item?.GetComponent<RuneSlot>();
      if (runeSlot != null)
      {
        runeSlot.PlaySelect();

        var args = new RuneEquipArgs(
            runeSlot.SlotId,
            runeSlot.RuneName,
            runeSlot.RunePowers,
            runeSlot.SpecialPowerName,
            runeSlot.SpecialPowerDescription
        );
        EventChannelLocator.MainContainer.runeInfoChannel.Raise(args);

        OnRuneItemSelected?.Invoke(runeSlot);
      }
    }

    // ──────────────────────────────────────────────
    //  装备插槽选择
    // ──────────────────────────────────────────────

    private void OnEquipSlotSelected(RuneEquip equip)
    {
      if (equip == null || equip == selectedEquip)
        return;

      // 取消选中旧插槽
      selectedEquip?.SetSelected(false);

      // 选中新插槽
      selectedEquip = equip;
      selectedEquip.SetSelected(true);
    }

    // ──────────────────────────────────────────────
    //  装备符文
    // ──────────────────────────────────────────────

    private void OnEquipClicked()
    {
      if (selectedRuneListItem == null)
      {
        Debug.LogWarning("[RunePanel] 没有选中的符文图标");
        return;
      }

      var runeSlot = selectedRuneListItem.GetComponent<RuneSlot>();
      if (runeSlot == null)
      {
        Debug.LogWarning("[RunePanel] 选中的符文图标没有 RuneSlot 组件");
        return;
      }

      if (selectedEquip == null)
      {
        Debug.LogWarning("[RunePanel] 没有选中的装备插槽");
        return;
      }

      if (EquipRune(runeSlot, selectedEquip))
      {
        // 装备成功 → 刷新标记并重排
        RefreshAllEquippedMarks();
        ReorderEquippedToFront();
      }
    }

    /// <summary>
    /// 执行装备 / 调换逻辑，供点击装备按钮和拖拽丢落共用
    /// </summary>
    /// <returns>true = 装备成功 / 调换成功</returns>
    public bool EquipRune(RuneSlot runeSlot, RuneEquip equipSlot)
    {
      if (runeSlot == null || equipSlot == null)
      {
        Debug.LogWarning("[RunePanel] EquipRune 参数为空");
        return false;
      }

      if (runeSlot.RuneData == null)
      {
        Debug.LogWarning("[RunePanel] 符文数据为空，无法装备");
        return false;
      }

      int newRuneId = runeSlot.RuneData.runeId;

      // 情况1：该符文已装备在当前插槽 → 无效果
      if (equipSlot.EquippedRuneId == newRuneId)
      {
        Debug.Log("[RunePanel] 该符文已装备在选中插槽，无变化");
        return false;
      }

      if (runeSlot.RuneData.icon == null)
      {
        Debug.LogWarning("[RunePanel] 符文没有图标数据");
        return false;
      }

      Sprite newIcon = runeSlot.RuneData.icon;
      RuneEquip otherEquip = (equipSlot == runeEquip1) ? runeEquip2 : runeEquip1;

      // 情况2：该符文已装备在另一个插槽 → 调换两插槽的符文
      if (otherEquip != null && otherEquip.EquippedRuneId == newRuneId)
      {
        int swappedRuneId = equipSlot.EquippedRuneId;
        Sprite swappedIcon = equipSlot.EquippedIcon;
        RuneSlot swappedSlot = equipSlot.EquippedRuneSlot;

        // 选中插槽 ← 新符文
        equipSlot.Equip(newRuneId, newIcon, runeSlot);

        if (swappedRuneId != -1)
        {
          // 另一插槽 ← 旧符文（调换）
          otherEquip.Equip(swappedRuneId, swappedIcon, swappedSlot);
          OnRuneEquipped?.Invoke(otherEquip.EquipIndex, swappedIcon);
        }
        else
        {
          // 另一插槽被腾空（移动而非调换）
          otherEquip.Unequip();
          OnRuneUnequipped?.Invoke(otherEquip.EquipIndex);
        }

        // 通知 LobbyCanvas 更新选中插槽图标
        OnRuneEquipped?.Invoke(equipSlot.EquipIndex, newIcon);

        Debug.Log($"[RunePanel] 调换符文: 插槽{equipSlot.EquipIndex}←{newRuneId}, 插槽{otherEquip.EquipIndex}←{swappedRuneId}");
      }
      else
      {
        // 情况3：全新装备 → 直接装备到选中插槽
        equipSlot.Equip(newRuneId, newIcon, runeSlot);
        ApplyEquipEffect(newRuneId);

        // 通知 LobbyCanvas
        OnRuneEquipped?.Invoke(equipSlot.EquipIndex, newIcon);

        Debug.Log($"[RunePanel] 装备符文: 插槽{equipSlot.EquipIndex} ← {runeSlot.RuneName}");
      }

      return true;
    }

    /// <summary>应用装备效果（留空，仅记录装备符文，进入游戏时调用）</summary>
    private void ApplyEquipEffect(int runeId)
    {
      // TODO: 实际游戏效果在进入游戏时根据 EquippedRuneId 应用
    }

    // ──────────────────────────────────────────────
    //  分解重复符文
    // ──────────────────────────────────────────────

    private void OnBreakdownClicked()
    {
      int beforeCount = ownedRuneIds.Count;

      // 1. 去重 DefaultRuneIds（永久生效，分解后不再恢复重复）
      var dedupedDefaults = new List<int>();
      var seenDefaults = new HashSet<int>();
      foreach (int id in DefaultRuneIds)
      {
        if (seenDefaults.Add(id))
          dedupedDefaults.Add(id);
      }
      DefaultRuneIds = dedupedDefaults;

      // 2. 去重 RuneInventory（PlayerPrefs 持久化）
      RuneInventory.BreakdownDuplicates();

      // 3. 重建 ownedRuneIds
      MergeInventoryRunes();

      int removedCount = beforeCount - ownedRuneIds.Count;
      if (removedCount == 0)
      {
        Debug.Log("[RunePanel] 没有可分解的重复符文");
        return;
      }

      // 每个分解的符文返还 30 金币
      int reward = removedCount * 30;
      if (ServiceLocator.Get<CoinManager>() != null)
        ServiceLocator.Get<CoinManager>().AddCoins(reward);

      AudioManager.Instance?.PlayUI("sfx_Coin");

      // 4. 重建符文列表
      BuildRuneSlotList();
      RestoreEquipReferences();
      RefreshAllEquippedMarks();
      ReorderEquippedToFront();

      // 5. 重新选中列表第一项
      DeselectAllItems();
      if (runeSlotList.Count > 0)
        SetRuneSlotItemSelected(runeSlotList[0]);

      Debug.Log($"[RunePanel] 分解完成：移除 {removedCount} 个重复符文，获得 {reward} 金币");

      // 通知 TopNotice 显示分解成功
      var topNotice = FindObjectOfType<UI.Framework.TopNotice>();
      if (topNotice != null)
        topNotice.Show("分解成功");
    }

    // ──────────────────────────────────────────────
    //  已装备标记刷新
    // ──────────────────────────────────────────────

    /// <summary>遍历所有 RuneSlot，刷新已装备标记（按实例精确匹配）</summary>
    private void RefreshAllEquippedMarks()
    {
      foreach (var go in runeSlotList)
      {
        var runeSlot = go.GetComponent<RuneSlot>();
        if (runeSlot == null) continue;

        bool isEquipped = (runeEquip1 != null && runeEquip1.EquippedRuneSlot == runeSlot) ||
                          (runeEquip2 != null && runeEquip2.EquippedRuneSlot == runeSlot);
        runeSlot.SetEquipped(isEquipped);
      }
    }

    /// <summary>将已装备的符文插槽移到列表最前面</summary>
    private void ReorderEquippedToFront()
    {
      // 收集已装备的 GameObject
      var equippedGOs = new List<GameObject>();
      var unequippedGOs = new List<GameObject>();
      foreach (var go in runeSlotList)
      {
        if (go == null) continue;
        var runeSlot = go.GetComponent<RuneSlot>();
        if (runeSlot == null) continue;

        bool isEquipped = (runeEquip1 != null && runeEquip1.EquippedRuneSlot == runeSlot) ||
                          (runeEquip2 != null && runeEquip2.EquippedRuneSlot == runeSlot);
        if (isEquipped)
          equippedGOs.Add(go);
        else
          unequippedGOs.Add(go);
      }

      // 按顺序设置 hierarchy 层级
      runeSlotList.Clear();
      int siblingIndex = 0;
      foreach (var go in equippedGOs)
      {
        go.transform.SetSiblingIndex(siblingIndex++);
        runeSlotList.Add(go);
      }
      foreach (var go in unequippedGOs)
      {
        go.transform.SetSiblingIndex(siblingIndex++);
        runeSlotList.Add(go);
      }
    }

    // ──────────────────────────────────────────────
    //  外部接口
    // ──────────────────────────────────────────────

    /// <summary>获取当前选中的装备插槽</summary>
    public RuneEquip GetSelectedEquip() => selectedEquip;
    public RuneEquip GetEquip1() => runeEquip1;
    public RuneEquip GetEquip2() => runeEquip2;

    /// <summary>由 LobbyCanvas 调用，选中指定索引的装备插槽 (0/1)</summary>
    public void SelectEquipSlot(int index)
    {
      var target = index == 0 ? runeEquip1 : runeEquip2;
      if (target != null)
        OnEquipSlotSelected(target);
    }

    // ──────────────────────────────────────────────
    //  取消所有选中
    // ──────────────────────────────────────────────

    private void DeselectAllItems()
    {
      // ForceDeselect 所有 RuneSlot
      var slots = GetComponentsInChildren<RuneSlot>(true);
      foreach (var slot in slots)
      {
        if (slot != null)
          slot.ForceDeselect();
      }

      selectedRuneListItem = null;
      selectedEquip?.SetSelected(false);
      selectedEquip = null;
    }

    // ──────────────────────────────────────────────
    //  拖拽丢落处理
    // ──────────────────────────────────────────────

    private void OnDestroy()
    {
      // 取消订阅事件，防止内存泄漏
      RuneSlot.OnDragStarted -= OnAnyRuneDragStarted;
      RuneSlot.OnDragEnded -= OnAnyRuneDragEnded;

      if (runeEquip1 != null)
      {
        runeEquip1.OnSelected -= OnEquipSlotSelected;
        runeEquip1.OnDropReceived -= OnRuneDropReceived;
      }
      if (runeEquip2 != null)
      {
        runeEquip2.OnSelected -= OnEquipSlotSelected;
        runeEquip2.OnDropReceived -= OnRuneDropReceived;
      }
    }

    /// <summary>拖拽开始时高亮装备插槽，提示可拖放区域</summary>
    private void OnAnyRuneDragStarted(RuneSlot runeSlot)
    {
      SetEquipSlotsHighlight(true);
    }

    /// <summary>拖拽结束时取消装备插槽高亮</summary>
    private void OnAnyRuneDragEnded(RuneSlot runeSlot)
    {
      SetEquipSlotsHighlight(false);
    }

    /// <summary>符文拖拽丢落到装备插槽上时执行装备</summary>
    private void OnRuneDropReceived(RuneEquip equip, RuneSlot runeSlot)
    {
      if (EquipRune(runeSlot, equip))
      {
        // 装备成功 → 刷新标记并重排
        RefreshAllEquippedMarks();
        ReorderEquippedToFront();

        // 选中该装备插槽（同步 UI 选中状态）
        OnEquipSlotSelected(equip);
      }
    }

    /// <summary>切换装备插槽的高亮状态</summary>
    private void SetEquipSlotsHighlight(bool highlight)
    {
      if (runeEquip1 != null)
        runeEquip1.SetHighlight(highlight, dragTargetHighlightColor);
      if (runeEquip2 != null)
        runeEquip2.SetHighlight(highlight, dragTargetHighlightColor);
    }
  }
}