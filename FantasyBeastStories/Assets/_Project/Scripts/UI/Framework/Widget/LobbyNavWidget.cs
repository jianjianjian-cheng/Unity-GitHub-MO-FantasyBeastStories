using System.Collections.Generic;
using Core;
using Core.Channels.General;
using DG.Tweening;
using UI.Framework.Base;
using UI.Framework.Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace UI.Framework.Widget
{
    /// <summary>
    /// 大厅导航组件：管理顶部导航按钮的选中状态、面板开关、模糊效果和 ESC 处理。
    /// </summary>
    public class LobbyNavWidget : UIWidget
    {
        private const string CharactorPanelId = "CharactorPanel";
        private const string RunePanelId = "RunePanel";
        private const string MassionPanelId = "MassionPanel";
        private const string ShopPanelId = "ShopPanel";

        [Header("导航按钮")]
        [SerializeField] private Button lobbyNavButton;
        [SerializeField] private Button characterNavButton;
        [SerializeField] private Button runeNavButton;
        [SerializeField] private Button missionNavButton;
        [SerializeField] private Button shopNavButton;

        [Header("后处理")]
        [SerializeField] private Volume postProcessVolume;

        private Sprite _selectedButtonImage;
        private Sprite _defaultButtonImage;

        protected override void AutoBindComponents()
        {
            _selectedButtonImage = AssetLoader.LoadAsset<Sprite>("UI/SelectedButton");
            _defaultButtonImage = AssetLoader.LoadAsset<Sprite>("UI/DefaultButton");
        }

        protected override void SubscribeEvents()
        {
            if (lobbyNavButton != null)
                lobbyNavButton.onClick.AddListener(OnLobbyNavClicked);
            if (characterNavButton != null)
                characterNavButton.onClick.AddListener(OnCharacterNavClicked);
            if (runeNavButton != null)
                runeNavButton.onClick.AddListener(OnRuneNavClicked);
            if (missionNavButton != null)
                missionNavButton.onClick.AddListener(OnMissionNavClicked);
            if (shopNavButton != null)
                shopNavButton.onClick.AddListener(OnShopNavClicked);
        }

        protected override void UnsubscribeEvents()
        {
            if (lobbyNavButton != null)
                lobbyNavButton.onClick.RemoveListener(OnLobbyNavClicked);
            if (characterNavButton != null)
                characterNavButton.onClick.RemoveListener(OnCharacterNavClicked);
            if (runeNavButton != null)
                runeNavButton.onClick.RemoveListener(OnRuneNavClicked);
            if (missionNavButton != null)
                missionNavButton.onClick.RemoveListener(OnMissionNavClicked);
            if (shopNavButton != null)
                shopNavButton.onClick.RemoveListener(OnShopNavClicked);
        }

        public override void OnScreenOpened()
        {
            SetButtonSelected(lobbyNavButton?.gameObject);
        }

        protected virtual void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                HandleEscape();
        }

        // ──────────────────────────────────────────────
        //  导航按钮事件
        // ──────────────────────────────────────────────

        private void OnLobbyNavClicked()
        {
            SetButtonSelected(lobbyNavButton?.gameObject);
            CloseAllPanels();
        }

        private void OnCharacterNavClicked()
        {
            ClosePanel(RunePanelId);
            ClosePanel(MassionPanelId);
            ClosePanel(ShopPanelId);
            SetButtonSelected(characterNavButton?.gameObject);
            OpenPanel(CharactorPanelId);
        }

        private void OnRuneNavClicked()
        {
            ClosePanel(CharactorPanelId);
            ClosePanel(MassionPanelId);
            ClosePanel(ShopPanelId);
            SetButtonSelected(runeNavButton?.gameObject);
            OpenPanel(RunePanelId);
        }

        private void OnMissionNavClicked()
        {
            ClosePanel(CharactorPanelId);
            ClosePanel(RunePanelId);
            ClosePanel(ShopPanelId);
            SetButtonSelected(missionNavButton?.gameObject);
            OpenPanel(MassionPanelId);
        }

        private void OnShopNavClicked()
        {
            ClosePanel(CharactorPanelId);
            ClosePanel(RunePanelId);
            ClosePanel(MassionPanelId);
            SetButtonSelected(shopNavButton?.gameObject);
            OpenPanel(ShopPanelId);
        }

        // ──────────────────────────────────────────────
        //  面板开关
        // ──────────────────────────────────────────────

        private void OpenPanel(string panelId)
        {
            var panel = UIManager.Instance.GetScreen(panelId);
            if (panel == null)
            {
                Debug.LogError($"[LobbyNavWidget] 未找到面板 {panelId}");
                return;
            }
            panel.Open();
            SetBlurAndRotation(true);
        }

        private void ClosePanel(string panelId)
        {
            var panel = UIManager.Instance.GetScreen(panelId);
            if (panel == null) return;
            panel.Close();
            SetBlurAndRotation(false);
        }

        private void CloseAllPanels()
        {
            ClosePanel(CharactorPanelId);
            ClosePanel(RunePanelId);
            ClosePanel(MassionPanelId);
            ClosePanel(ShopPanelId);
        }

        private void HandleEscape()
        {
            SetButtonSelected(lobbyNavButton?.gameObject);
            CloseAllPanels();
        }

        // ──────────────────────────────────────────────
        //  模糊 / 旋转
        // ──────────────────────────────────────────────

        private void SetBlurAndRotation(bool anyOpen)
        {
            if (postProcessVolume != null)
                postProcessVolume.weight = anyOpen ? 1f : 0f;

            EventChannelLocator.MainContainer.changeCanRotateChannel.Raise(anyOpen);
        }

        // ──────────────────────────────────────────────
        //  按钮选中状态
        // ──────────────────────────────────────────────

        private void SetButtonSelected(GameObject button)
        {
            if (button == null) return;
            EventSystem.current?.SetSelectedGameObject(button);

            SetNavButtonState(lobbyNavButton, button);
            SetNavButtonState(characterNavButton, button);
            SetNavButtonState(runeNavButton, button);
            SetNavButtonState(missionNavButton, button);
            SetNavButtonState(shopNavButton, button);
        }

        private void SetNavButtonState(Button btn, GameObject selected)
        {
            if (btn == null) return;
            bool isSelected = btn.gameObject == selected;
            btn.interactable = !isSelected;
            var img = btn.GetComponent<Image>();
            if (img != null)
                img.sprite = isSelected ? _selectedButtonImage : _defaultButtonImage;
        }
    }
}
