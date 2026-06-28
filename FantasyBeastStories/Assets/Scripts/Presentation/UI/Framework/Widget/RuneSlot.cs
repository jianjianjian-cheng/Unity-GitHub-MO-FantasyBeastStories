using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Presentation.UI.Framework.Base;

namespace Presentation.UI.Framework
{
    public class RuneSlot : UIWidget, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("RuneSlot UI")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameText;

        [Header("RuneSlot Data")]
        [SerializeField] private int slotId;

        /// <summary>点击事件，供 RunePanel 订阅</summary>
        public event Action<RuneSlot> OnClicked;

        // 通过 Setup() 注入的符文数据资产
        private RuneDataSO runeData;

        // ── 只读公开属性 ──

        public int SlotId => slotId;
        public RuneDataSO RuneData => runeData;

        /// <summary>向后兼容：从 runeData.powers 构建字典</summary>
        public Dictionary<int, string> RunePowers
        {
            get
            {
                var dict = new Dictionary<int, string>();
                if (runeData != null)
                    foreach (var p in runeData.powers)
                        dict[p.value] = p.label;
                return dict;
            }
        }

        public string RuneName => runeData != null ? runeData.runeName : string.Empty;
        public string SpecialPowerName => runeData != null ? runeData.specialPowerName : string.Empty;
        public string SpecialPowerDescription => runeData != null ? runeData.specialPowerDescription : string.Empty;

        // ── 外部注入 ──

        /// <summary>用 RuneDataSO 设置该槽位的符文数据</summary>
        public void Setup(RuneDataSO data)
        {
            runeData = data;

            if (data == null)
            {
                if (iconImage != null) iconImage.sprite = null;
                if (nameText != null) nameText.text = string.Empty;
                return;
            }

            slotId = data.runeId;

            if (iconImage != null)
                iconImage.sprite = data.icon;

            if (nameText != null)
                nameText.text = data.runeName;
        }

        // ── UIWidget 生命周期 ──

        protected override void AutoBindComponents()
        {
            // 自动查找 Icon 子对象
            if (iconImage == null)
            {
                var iconTr = transform.Find("Icon");
                if (iconTr != null)
                    iconImage = iconTr.GetComponent<Image>();
            }

            // 自动查找 Name 子对象
            if (nameText == null)
            {
                var nameTr = transform.Find("Name");
                if (nameTr != null)
                    nameText = nameTr.GetComponent<Text>();
            }

            // 确保自身 Image 可接收射线
            var image = GetComponent<Image>();
            if (image != null)
                image.raycastTarget = true;
        }

        // ── 指针事件（点击 + 悬浮） ──

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClicked?.Invoke(this);
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
}