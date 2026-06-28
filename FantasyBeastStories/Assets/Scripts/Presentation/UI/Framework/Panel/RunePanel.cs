using System;
using System.Collections.Generic;
using Domain.Event;
using Domain.Event.Channels.General;
using Presentation.UI.Framework;
using Presentation.UI.Framework.Base;
using Presentation.UI.Framework.Manager;
using UnityEngine;
using UnityEngine.UI;

public class RunePanel : UIScreen
{
    [Header("Rune Panel Buttons")]
    [SerializeField] private Button equipButton;

    [Header("符文列表（动态生成）")]
    [SerializeField] private GameObject runeSlotPrefab;       // 符文插槽预制体
    [SerializeField] private GameObject runeSlotListPanel;    // 符文列表父级 Panel（生成的预制体挂在此 Panel 下）

    [Header("符文装备插槽")]
    [SerializeField] private RuneEquip runeEquip1;
    [SerializeField] private RuneEquip runeEquip2;

    // 运行时动态生成的符文插槽列表
    private List<GameObject> runeSlotList = new List<GameObject>();

    // 选中状态
    private GameObject selectedRuneListItem;
    private RuneEquip selectedEquip; // 当前选中的装备插槽

    // ── 玩家拥有的符文（后续从存档加载） ──
    private List<int> ownedRuneIds = new List<int>();

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
        // 初始化已拥有符文（后续从存档加载）
        ownedRuneIds = new List<int> { 0, 0, 1, 1, 1 };

        // 绑定装备按钮
        if (equipButton != null)
            equipButton.onClick.AddListener(OnEquipClicked);

        // 动态生成符文列表（根据 ownedRuneIds 过滤 RuneDatabase）
        BuildRuneSlotList();

        // 默认装备：RuneEquip_1 ← RuneSlot[0], RuneEquip_2 ← RuneSlot[1]
        EquipDefaultRunes();

        // 订阅装备插槽选中事件，默认选中插槽1
        if (runeEquip1 != null)
        {
            runeEquip1.OnSelected += OnEquipSlotSelected;
            selectedEquip = runeEquip1;
            runeEquip1.SetSelected(true);
        }
        if (runeEquip2 != null)
            runeEquip2.OnSelected += OnEquipSlotSelected;
    }

    protected override void OnBeforeOpen()
    {
        base.OnBeforeOpen();

        // 默认选中符文槽第 1 项
        if (selectedRuneListItem == null && runeSlotList.Count > 0)
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

        // 从 Resources 加载符文数据库，构建 runeId → RuneDataSO 的快速查找
        var database = Resources.Load<RuneDatabaseSO>("RuneData/RuneDatabase");
        if (database == null || database.allRunes.Count == 0)
        {
            Debug.LogWarning("[RunePanel] 未找到 RuneDatabase 或数据为空");
            return;
        }

        // 构建 runeId → RuneDataSO 字典
        var runeDataMap = new Dictionary<int, RuneDataSO>();
        foreach (var data in database.allRunes)
            if (data != null && !runeDataMap.ContainsKey(data.runeId))
                runeDataMap[data.runeId] = data;

        // 对 ownedRuneIds 排序（自动按 runeId 升序排列）
        ownedRuneIds.Sort();

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
                equip.Equip(runeId, slot.RuneData.icon);
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
            prevSlot?.PlayDeselect();
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

        int newRuneId = runeSlot.RuneData != null ? runeSlot.RuneData.runeId : runeSlot.SlotId;

        // 情况1：该符文已装备在当前选中插槽 → 无效果
        if (selectedEquip.EquippedRuneId == newRuneId)
        {
            Debug.Log("[RunePanel] 该符文已装备在选中插槽，无变化");
            return;
        }

        // 获取被选符文的图标（直接从 RuneDataSO 获取，无需查找子对象）
        if (runeSlot.RuneData == null || runeSlot.RuneData.icon == null)
        {
            Debug.LogWarning("[RunePanel] 选中符文没有图标数据");
            return;
        }

        Sprite newIcon = runeSlot.RuneData.icon;
        RuneEquip otherEquip = (selectedEquip == runeEquip1) ? runeEquip2 : runeEquip1;

        // 情况2：该符文已装备在另一个插槽 → 调换两插槽的符文
        if (otherEquip != null && otherEquip.EquippedRuneId == newRuneId)
        {
            int swappedRuneId = selectedEquip.EquippedRuneId;
            Sprite swappedIcon = selectedEquip.EquippedIcon;

            // 选中插槽 ← 新符文
            selectedEquip.Equip(newRuneId, newIcon);

            if (swappedRuneId != -1)
            {
                // 另一插槽 ← 旧符文（调换）
                otherEquip.Equip(swappedRuneId, swappedIcon);
                OnRuneEquipped?.Invoke(otherEquip.EquipIndex, swappedIcon);
            }
            else
            {
                // 另一插槽被腾空（移动而非调换）
                otherEquip.Unequip();
                OnRuneUnequipped?.Invoke(otherEquip.EquipIndex);
            }

            // 通知 LobbyCanvas 更新选中插槽图标
            OnRuneEquipped?.Invoke(selectedEquip.EquipIndex, newIcon);

            Debug.Log($"[RunePanel] 调换符文: 插槽{selectedEquip.EquipIndex}←{newRuneId}, 插槽{otherEquip.EquipIndex}←{swappedRuneId}");
        }
        else
        {
            // 情况3：全新装备 → 直接装备到选中插槽
            selectedEquip.Equip(newRuneId, newIcon);
            ApplyEquipEffect(newRuneId);

            // 通知 LobbyCanvas
            OnRuneEquipped?.Invoke(selectedEquip.EquipIndex, newIcon);

            Debug.Log($"[RunePanel] 装备符文: 插槽{selectedEquip.EquipIndex} ← {runeSlot.RuneName}");
        }
    }

    /// <summary>应用装备效果（留空，仅记录装备符文，进入游戏时调用）</summary>
    private void ApplyEquipEffect(int runeId)
    {
        // TODO: 实际游戏效果在进入游戏时根据 EquippedRuneId 应用
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
        selectedRuneListItem = null;
        selectedEquip?.SetSelected(false);
        selectedEquip = null;
    }
}