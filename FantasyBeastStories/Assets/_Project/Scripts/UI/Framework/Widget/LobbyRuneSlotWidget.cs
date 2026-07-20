using System.Collections.Generic;
using Core;
using DG.Tweening;
using UI.Framework.Base;
using UI.Framework.Manager;
using UI.Framework.Panel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UI.Input;

namespace UI.Framework.Widget
{
    /// <summary>
    /// 大厅符文插槽组件：管理两个符文插槽的选择、图标更新、悬浮动画。
    /// 通过 RunePanel 事件回调更新图标。
    /// </summary>
    public class LobbyRuneSlotWidget : UIWidget
    {
        private const string RunePanelId = "RunePanel";

        [Header("符文插槽")]
        [SerializeField] private Button runeSlot1Button;
        [SerializeField] private Button runeSlot2Button;

        private Transform _runeSlot1Icon;
        private Transform _runeSlot2Icon;
        private GameObject _selectedRuneIcon;

        private Sprite _selectedButtonImage;
        private Sprite _defaultButtonImage;

        protected override void AutoBindComponents()
        {
            if (runeSlot1Button != null)
                _runeSlot1Icon = runeSlot1Button.transform.Find("Icon");
            if (runeSlot2Button != null)
                _runeSlot2Icon = runeSlot2Button.transform.Find("Icon");

            _selectedButtonImage = AssetLoader.LoadAsset<Sprite>("UI/SelectedButton");
            _defaultButtonImage = AssetLoader.LoadAsset<Sprite>("UI/DefaultButton");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToRunePanel();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnsubscribeFromRunePanel();
        }

        protected override void SubscribeEvents()
        {
            if (runeSlot1Button != null)
            {
                runeSlot1Button.onClick.AddListener(OnRuneSlot1Clicked);
                AddRuneSlotHoverAnimation(runeSlot1Button.gameObject);
            }
            if (runeSlot2Button != null)
            {
                runeSlot2Button.onClick.AddListener(OnRuneSlot2Clicked);
                AddRuneSlotHoverAnimation(runeSlot2Button.gameObject);
            }
        }

        protected override void UnsubscribeEvents()
        {
            if (runeSlot1Button != null)
                runeSlot1Button.onClick.RemoveListener(OnRuneSlot1Clicked);
            if (runeSlot2Button != null)
                runeSlot2Button.onClick.RemoveListener(OnRuneSlot2Clicked);
        }

        protected virtual void Update()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0)
                && _selectedRuneIcon != null
                && CanDeselectRuneIcons()
                && !IsPointerOverRuneSlot())
            {
                DeselectRuneIcons();
            }
        }

        // ──────────────────────────────────────────────
        //  RunePanel 事件对接
        // ──────────────────────────────────────────────

        private void SubscribeToRunePanel()
        {
            var runePanel = UIManager.HasInstance
                ? UIManager.Instance.GetScreen(RunePanelId) as RunePanel
                : null;
            if (runePanel != null)
            {
                runePanel.OnRuneEquipped += OnRuneEquipped;
                runePanel.OnRuneUnequipped += OnRuneUnequipped;
            }
        }

        private void UnsubscribeFromRunePanel()
        {
            if (!UIManager.HasInstance) return;
            var runePanel = UIManager.Instance.GetScreen(RunePanelId) as RunePanel;
            if (runePanel != null)
            {
                runePanel.OnRuneEquipped -= OnRuneEquipped;
                runePanel.OnRuneUnequipped -= OnRuneUnequipped;
            }
        }

        // ──────────────────────────────────────────────
        //  插槽点击
        // ──────────────────────────────────────────────

        private void OnRuneSlot1Clicked()
        {
            CloseOtherPanels();
            SetRuneIconSelected(runeSlot1Button?.gameObject);
            OpenRunePanel();
            NotifyRunePanelSlot(0);
        }

        private void OnRuneSlot2Clicked()
        {
            CloseOtherPanels();
            SetRuneIconSelected(runeSlot2Button?.gameObject);
            OpenRunePanel();
            NotifyRunePanelSlot(1);
        }

        private void CloseOtherPanels()
        {
            ClosePanel("CharactorPanel");
            ClosePanel("ShopPanel");
        }

        private void OpenRunePanel()
        {
            var runePanel = UIManager.Instance.GetScreen(RunePanelId) as RunePanel;
            if (runePanel == null)
            {
                Debug.LogError($"[LobbyRuneSlotWidget] 未找到 {RunePanelId}");
                return;
            }
            if (!runePanel.IsOpen)
                runePanel.Open();
        }

        private void NotifyRunePanelSlot(int slotIndex)
        {
            var runePanel = UIManager.Instance.GetScreen(RunePanelId) as RunePanel;
            if (runePanel != null)
                runePanel.SelectEquipSlot(slotIndex);
        }

        private void ClosePanel(string panelId)
        {
            var panel = UIManager.Instance.GetScreen(panelId);
            if (panel != null)
                panel.Close();
        }

        // ──────────────────────────────────────────────
        //  RunePanel 回调
        // ──────────────────────────────────────────────

        private void OnRuneEquipped(int slotIndex, Sprite equippedSprite)
        {
            Transform targetIcon = slotIndex == 0 ? _runeSlot1Icon : _runeSlot2Icon;
            if (targetIcon != null)
            {
                var img = targetIcon.GetComponent<Image>();
                if (img != null)
                    img.sprite = equippedSprite;
            }
        }

        private void OnRuneUnequipped(int slotIndex)
        {
            Transform targetIcon = slotIndex == 0 ? _runeSlot1Icon : _runeSlot2Icon;
            if (targetIcon != null)
            {
                var img = targetIcon.GetComponent<Image>();
                if (img != null)
                    img.sprite = null;
            }
        }

        // ──────────────────────────────────────────────
        //  插槽选中状态
        // ──────────────────────────────────────────────

        private void SetRuneIconSelected(GameObject icon)
        {
            _selectedRuneIcon = icon;

            if (runeSlot1Button != null)
            {
                runeSlot1Button.GetComponent<Image>().sprite =
                    icon == runeSlot1Button.gameObject ? _selectedButtonImage : _defaultButtonImage;
                runeSlot1Button.transform.DOScale(icon == runeSlot1Button.gameObject ? 1.1f : 1f, 0.2f);
            }

            if (runeSlot2Button != null)
            {
                runeSlot2Button.GetComponent<Image>().sprite =
                    icon == runeSlot2Button.gameObject ? _selectedButtonImage : _defaultButtonImage;
                runeSlot2Button.transform.DOScale(icon == runeSlot2Button.gameObject ? 1.1f : 1f, 0.2f);
            }
        }

        private void DeselectRuneIcons()
        {
            if (_selectedRuneIcon == null) return;
            _selectedRuneIcon = null;

            if (runeSlot1Button != null)
            {
                runeSlot1Button.GetComponent<Image>().sprite = _defaultButtonImage;
                runeSlot1Button.transform.DOScale(1f, 0.2f);
            }

            if (runeSlot2Button != null)
            {
                runeSlot2Button.GetComponent<Image>().sprite = _defaultButtonImage;
                runeSlot2Button.transform.DOScale(1f, 0.2f);
            }
        }

        private bool CanDeselectRuneIcons()
        {
            if (!UIManager.HasInstance) return false;
            var runePanel = UIManager.Instance.GetScreen(RunePanelId);
            return runePanel == null || (!runePanel.IsOpen && !runePanel.IsAnimating);
        }

        private bool IsPointerOverRuneSlot()
        {
            if (EventSystem.current == null) return false;

            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = MobileInputHelper.GetScreenPosition(),
            };

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                GameObject go = result.gameObject;
                if (go == runeSlot1Button?.gameObject
                    || go == runeSlot2Button?.gameObject
                    || (runeSlot1Button != null && go.transform.IsChildOf(runeSlot1Button.transform))
                    || (runeSlot2Button != null && go.transform.IsChildOf(runeSlot2Button.transform)))
                {
                    return true;
                }
            }
            return false;
        }

        // ──────────────────────────────────────────────
        //  悬浮动画
        // ──────────────────────────────────────────────

        private void AddRuneSlotHoverAnimation(GameObject slot)
        {
            var trigger = slot.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = slot.AddComponent<EventTrigger>();

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => slot.transform.DOScale(1.1f, 0.2f));
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ =>
            {
                if (_selectedRuneIcon != slot)
                    slot.transform.DOScale(1f, 0.2f);
            });
            trigger.triggers.Add(exit);
        }
    }
}
