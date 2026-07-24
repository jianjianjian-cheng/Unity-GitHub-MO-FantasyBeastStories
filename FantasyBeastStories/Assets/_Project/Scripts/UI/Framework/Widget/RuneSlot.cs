using System;
using System.Collections.Generic;
using Controllers.Rune;
using Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UI.Framework.Base;

namespace UI.Framework
{
    public class RuneSlot : UIWidget, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
                             IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("RuneSlot UI")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameText;

        [Header("RuneSlot Data")]
        [SerializeField] private int slotId;

        [Header("选中高亮")]
        [SerializeField] private Image bgcImage; // BGC 子对象的 Image（拖拽赋值）
        [SerializeField] private Sprite selectedSprite;   // 选中时的 BGC 图片
        [SerializeField] private Sprite deselectedSprite; // 未选中时的 BGC 图片
        [SerializeField] private Sprite hoverSprite;      // 悬浮时的 BGC 图片

        [Header("已装备标记")]
        [SerializeField] private GameObject equippedMarkObject; // 已装备标记（含子对象 TextMeshProUGUI，拖拽赋值）

        [Header("拖拽")]
        [SerializeField][Range(0f, 1f)] private float dragGhostAlpha = 0.6f; // 拖拽虚影透明度

        /// <summary>点击事件，供 RunePanel 订阅</summary>
        public event Action<RuneSlot> OnClicked;

        /// <summary>开始拖拽符文时触发，供 RunePanel 订阅</summary>
        public static event Action<RuneSlot> OnDragStarted;

        /// <summary>拖拽结束时触发（无论是否落在有效区域），供 RunePanel 订阅</summary>
        public static event Action<RuneSlot> OnDragEnded;

        // 通过 Setup() 注入的符文数据资产
        private RuneDataSO runeData;

        // 新符文红点（动态创建）
        private GameObject _runeRedDot;

        /// <summary>该槽位是否有红点</summary>
        public bool HasRedDot => _runeRedDot != null && _runeRedDot.activeSelf;

        // 拖拽状态
        private Image dragGhostImage;
        private RectTransform dragGhostRect;
        private Canvas dragCanvas;

        // ── 只读公开属性 ──

        public int SlotId => slotId;
        public RuneDataSO RuneData => runeData;

        /// <summary>获取热更新后的有效数值（优先 Lua，回退 SO 默认值）</summary>
        public List<RunePower> RunePowers
        {
            get
            {
                if (runeData != null)
                    return RuneEffectApplier.GetEffectivePowers(runeData.runeId);
                return new List<RunePower>();
            }
        }

        public string RuneName => runeData != null ? runeData.runeName : string.Empty;
        public string SpecialPowerName => runeData != null
            ? RuneEffectApplier.GetEffectiveSpecialPowerName(runeData.runeId) : string.Empty;
        public string SpecialPowerDescription => runeData != null
            ? RuneEffectApplier.GetEffectiveSpecialPowerDescription(runeData.runeId) : string.Empty;

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

        /// <summary>设置已装备标记的显示状态</summary>
        public void SetEquipped(bool equipped)
        {
            if (equippedMarkObject != null)
                equippedMarkObject.SetActive(equipped);
        }

        // ──────────────────────────────────────────────
        //  新符文红点（由 RunePanel 外部控制）
        // ──────────────────────────────────────────────

        /// <summary>
        /// 设置红点可见性。由 RunePanel.BuildRuneSlotList 根据新增数量分配。
        /// ShopPanel 不调用此方法，因此商店中不会出现红点。
        /// </summary>
        public void SetRedDotVisible(bool visible)
        {
            if (runeData == null) return;

            if (visible)
            {
                if (_runeRedDot == null)
                    CreateRedDot();
                _runeRedDot.SetActive(true);
            }
            else if (_runeRedDot != null)
            {
                _runeRedDot.SetActive(false);
            }
        }

        private void CreateRedDot()
        {
            _runeRedDot = new GameObject("RuneRedDot");
            _runeRedDot.transform.SetParent(transform, false);

            var rt = _runeRedDot.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-12, -12);
            rt.sizeDelta = new Vector2(20, 20);

            var img = _runeRedDot.AddComponent<Image>();
            img.raycastTarget = false;

            // 通过 Addressables 加载红点精灵
            var sprite = AssetLoader.LoadAsset<Sprite>("Assets/_Project/Addressables/Sprites/UI/red_dot.png");
            if (sprite != null)
                img.sprite = sprite;
            else
                img.color = new Color(0.9f, 0.1f, 0.1f, 1f);

            _runeRedDot.SetActive(false);
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

            // 确保自身 Image 可接收射线，避免 prefab 根节点没有 Image 时无法触发点击
            var image = GetComponent<Image>();
            if (image == null)
            {
                image = gameObject.AddComponent<Image>();
                image.color = Color.clear;
            }
            image.raycastTarget = true;

            // 初始状态设为未选中图片
            if (bgcImage != null && deselectedSprite != null)
                bgcImage.sprite = deselectedSprite;

            // 已装备标记初始为不激活（由 SetEquipped 控制）
            if (equippedMarkObject != null)
                equippedMarkObject.SetActive(false);
        }

        // ──────────────────────────────────────────────
        //  选中 / 取消选中 → 切换 BGC 图片
        // ──────────────────────────────────────────────

        protected override void OnSelectCompleted()
        {
            if (bgcImage != null && selectedSprite != null)
                bgcImage.sprite = selectedSprite;
        }

        protected override void OnDeselectCompleted()
        {
            if (bgcImage != null && deselectedSprite != null)
                bgcImage.sprite = deselectedSprite;
        }

        /// <summary>手动取消选中（供 RunePanel.DeselectAllItems 调用）</summary>
        public void ForceDeselect()
        {
            if (IsSelected)
                PlayDeselect();
            // 立即切换图片，不依赖动画回调
            if (bgcImage != null && deselectedSprite != null)
                bgcImage.sprite = deselectedSprite;
        }

        // ── 指针事件（点击 + 悬浮） ──

        public void OnPointerClick(PointerEventData eventData)
        {
            // 清除该符文的新红点
            if (runeData != null && HasRedDot)
            {
                RuneInventory.ConsumeNew(runeData.runeId);
                SetRedDotVisible(false);
            }

            OnClicked?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PlayHoverEnter();

            // 未选中时切换为悬浮图片
            if (!IsSelected && bgcImage != null && hoverSprite != null)
                bgcImage.sprite = hoverSprite;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PlayHoverExit();

            // 恢复为对应状态的图片
            if (bgcImage == null) return;
            bgcImage.sprite = IsSelected ? selectedSprite : deselectedSprite;
        }

        // ── 拖拽（IBeginDragHandler / IDragHandler / IEndDragHandler） ──

        public void OnBeginDrag(PointerEventData eventData)
        {
            // 没有数据的符文不允许拖拽
            if (runeData == null || iconImage == null || iconImage.sprite == null)
                return;

            // 查找根 Canvas
            if (dragCanvas == null)
                dragCanvas = GetComponentInParent<Canvas>();
            if (dragCanvas == null) return;

            // 创建虚影 GameObject
            var ghostGO = new GameObject($"{name}_DragGhost", typeof(Image));
            ghostGO.transform.SetParent(dragCanvas.transform, false);
            ghostGO.transform.SetAsLastSibling();

            dragGhostImage = ghostGO.GetComponent<Image>();
            dragGhostImage.sprite = iconImage.sprite;
            dragGhostImage.raycastTarget = false;
            dragGhostImage.color = new Color(1f, 1f, 1f, dragGhostAlpha);

            dragGhostRect = ghostGO.GetComponent<RectTransform>();
            // 保持与原始图标一致的尺寸和比例
            dragGhostRect.sizeDelta = iconImage.rectTransform.sizeDelta;
            dragGhostRect.pivot = iconImage.rectTransform.pivot;

            // 初始位置对齐鼠标
            UpdateGhostPosition(eventData);

            // 通知 RunePanel 拖拽开始
            OnDragStarted?.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragGhostRect == null || dragCanvas == null) return;
            UpdateGhostPosition(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // 销毁虚影
            if (dragGhostImage != null)
            {
                Destroy(dragGhostImage.gameObject);
                dragGhostImage = null;
                dragGhostRect = null;
            }

            // 通知拖拽结束
            OnDragEnded?.Invoke(this);
        }

        /// <summary>将虚影移动到当前鼠标位置</summary>
        private void UpdateGhostPosition(PointerEventData eventData)
        {
            if (dragGhostRect == null || dragCanvas == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragCanvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var localPos
            );
            dragGhostRect.localPosition = localPos;
        }
    }
}
