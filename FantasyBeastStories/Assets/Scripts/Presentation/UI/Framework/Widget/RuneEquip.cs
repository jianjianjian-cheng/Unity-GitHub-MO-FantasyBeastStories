using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Presentation.UI.Framework.Base;

/// <summary>
/// 符文装备插槽（RuneEquip_1 / RuneEquip_2）
/// 可选中、可显示已装备的符文图标
/// </summary>
public class RuneEquip : UIWidget, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("RuneEquip")]
    [SerializeField] private int equipIndex; // 0 = 插槽1, 1 = 插槽2
    [SerializeField] private Image iconImage; // 显示符文图标的子对象

    public int EquipIndex => equipIndex;
    public int EquippedRuneId { get; private set; } = -1;
    public Sprite EquippedIcon { get; private set; }
    public bool HasRune => EquippedRuneId != -1;

    /// <summary>选中事件，供 RunePanel 订阅</summary>
    public event Action<RuneEquip> OnSelected;

    // ──────────────────────────────────────────────
    //  装备 / 卸下
    // ──────────────────────────────────────────────

    /// <summary>装备符文到该插槽</summary>
    public void Equip(int runeId, Sprite icon)
    {
        EquippedRuneId = runeId;
        EquippedIcon = icon;

        if (iconImage != null)
            iconImage.sprite = icon;
    }

    /// <summary>卸下当前符文</summary>
    public void Unequip()
    {
        EquippedRuneId = -1;
        EquippedIcon = null;

        if (iconImage != null)
            iconImage.sprite = null;
    }

    // ──────────────────────────────────────────────
    //  选中 / 取消选中
    // ──────────────────────────────────────────────

    /// <summary>设置该插槽的选中状态</summary>
    public void SetSelected(bool selected)
    {
        if (selected)
            PlaySelect();
        else
            PlayDeselect();
    }

    // ──────────────────────────────────────────────
    //  UIWidget
    // ──────────────────────────────────────────────

    protected override void AutoBindComponents()
    {
        // 自动查找 Icon 子对象
        if (iconImage == null)
        {
            var iconTr = transform.Find("Icon");
            if (iconTr != null)
                iconImage = iconTr.GetComponent<Image>();
        }

        // 确保自身 Image 可接收射线
        var img = GetComponent<Image>();
        if (img != null)
            img.raycastTarget = true;
    }

    // ──────────────────────────────────────────────
    //  点击 → 选中该插槽
    // ──────────────────────────────────────────────

    public void OnPointerClick(PointerEventData eventData)
    {
        OnSelected?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHoverEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlayHoverExit();
    }
}