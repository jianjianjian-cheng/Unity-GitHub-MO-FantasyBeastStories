using System;
using System.Collections.Generic;
using Domain.Event;
using Domain.Event.Channels.General;
using Presentation.UI;
using Presentation.UI.Framework.Base;
using Presentation.UI.Framework.Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RunePanel : UIScreen
{
    [Header("Rune Panel Buttons")]
    [SerializeField] private Button equipButton;
    [SerializeField] private List<GameObject> runeSlotList = new List<GameObject>();

    // 选中状态
    private GameObject selectedRuneListItem;
    private int[] currentEquippedRuneIds = new int[2];
    private int equipTargetSlot = 0; // 当前装备目标插槽（由 LobbyCanvas 设置）

    // --- 事件：通知 LobbyCanvas 更新图标视觉 ---
    /// <summary>符文装备事件 (slotIndex, equippedSprite)</summary>
    public event Action<int, Sprite> OnRuneEquipped;
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
        // 绑定装备按钮
        if (equipButton != null)
            equipButton.onClick.AddListener(OnEquipClicked);

        // 查找并绑定符文列表项
        FindRuneSlots();
    }

    protected override void OnBeforeOpen()
    {
        base.OnBeforeOpen();

        // 默认选中第一项（移到此处执行，避免 Awake 时对象未激活无法启动协程）
        if (selectedRuneListItem == null && runeSlotList.Count > 0)
            SetRuneSlotItemSelected(runeSlotList[0]);

        // 面板打开时无需额外动画
    }

    protected override void OnAfterClose()
    {
        base.OnAfterClose();
        DeselectAllItems();
    }

    // ──────────────────────────────────────────────
    //  符文插槽查找
    // ──────────────────────────────────────────────

    private void FindRuneSlots()
    {
        for (int i = 0; i < runeSlotList.Count; i++)
        {
            var slot = runeSlotList[i];
            if (slot == null) continue;

            var btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                int index = i;
                btn.onClick.AddListener(() => OnRuneSlotItemClicked(runeSlotList[index]));
            }
        }
    }

    // ──────────────────────────────────────────────
    //  符文列表选择
    // ──────────────────────────────────────────────

    private void OnRuneSlotItemClicked(GameObject item)
    {
        SetRuneSlotItemSelected(item);
    }

    private void SetRuneSlotItemSelected(GameObject item)
    {
        selectedRuneListItem = item;

        foreach (var slot in runeSlotList)
        {
            if (slot == null) continue;
            // 选中状态由外部 UI 动画控制
        }

        // 显示符文详情
        var runeSlot = item?.GetComponent<RuneSlot>();
        if (runeSlot != null)
        {
            var args = new RuneEquipArgs(
                runeSlot.slotId,
                runeSlot.RuneName,
                runeSlot.runePowers,
                runeSlot.specialPowerName,
                runeSlot.specialPowerDescription
            );
            EventChannelLocator.MainContainer.runeInfoChannel.Raise(args);

            OnRuneItemSelected?.Invoke(runeSlot);
        }
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

        Debug.Log($"[RunePanel] 装备符文: {runeSlot.RuneName}");

        var icon = selectedRuneListItem.transform.Find("Icon")?.GetComponent<Image>();
        if (icon == null || icon.sprite == null)
        {
            Debug.LogWarning("[RunePanel] 选中符文没有 Icon 子对象或 Sprite");
            return;
        }

        // 装备到目标插槽
        if (currentEquippedRuneIds[equipTargetSlot] == runeSlot.slotId)
        {
            Debug.LogWarning("[RunePanel] 该符文已装备在目标插槽");
            return;
        }

        currentEquippedRuneIds[equipTargetSlot] = runeSlot.slotId;

        // 通知 LobbyCanvas 更新插槽图标
        OnRuneEquipped?.Invoke(equipTargetSlot, icon.sprite);
    }

    // ──────────────────────────────────────────────
    //  外部接口
    // ──────────────────────────────────────────────

    /// <summary>由 LobbyCanvas 调用，设置装备目标插槽</summary>
    public void SetEquipTargetSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < currentEquippedRuneIds.Length)
            equipTargetSlot = slotIndex;
    }

    // ──────────────────────────────────────────────
    //  取消所有选中
    // ──────────────────────────────────────────────

    private void DeselectAllItems()
    {
        selectedRuneListItem = null;
    }
}