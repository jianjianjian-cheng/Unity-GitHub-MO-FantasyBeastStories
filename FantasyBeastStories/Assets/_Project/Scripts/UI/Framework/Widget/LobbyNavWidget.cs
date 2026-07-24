using Core;
using UI.Framework.Base;
using UI.Framework.Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace UI.Framework.Widget
{
    public class LobbyNavWidget : UIWidget
    {
        private const string CharactorPanelId = "CharactorPanel";
        private const string RunePanelId = "RunePanel";
        private const string MassionPanelId = "MassionPanel";
        private const string ShopPanelId = "ShopPanel";
        private const string SettingsPanelId = "SettingsPanel";

        [Header("导航按钮")]
        [SerializeField] private Button lobbyNavButton;
        [SerializeField] private Button characterNavButton;
        [SerializeField] private Button runeNavButton;
        [SerializeField] private Button missionNavButton;
        [SerializeField] private Button shopNavButton;
        [SerializeField] private Button settingsNavButton;

        [Header("后处理")]
        [SerializeField] private Volume postProcessVolume;

        private Sprite _selectedButtonImage;
        private Sprite _defaultButtonImage;
        private bool _blurActive;
        private string _lastActiveScreenId;

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
            if (settingsNavButton != null)
                settingsNavButton.onClick.AddListener(OnSettingsNavClicked);
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
            if (settingsNavButton != null)
                settingsNavButton.onClick.RemoveListener(OnSettingsNavClicked);
        }

        public override void OnScreenOpened()
        {
            SetButtonSelected(lobbyNavButton?.gameObject);
        }

        private void Update()
        {
            // 根据当前栈顶面板同步导航按钮选中状态
            if (!UIManager.HasInstance) return;

            var current = UIManager.Instance.GetCurrentScreen();
            string currentId = current?.ScreenId;

            // 栈空时回到大厅状态
            if (string.IsNullOrEmpty(currentId))
            {
                if (_blurActive)
                {
                    SetButtonSelected(lobbyNavButton?.gameObject);
                    SetBlurAndRotation(false);
                    _blurActive = false;
                }
                return;
            }

            // 栈顶变化时更新按钮选中
            if (_blurActive && currentId != _lastActiveScreenId)
            {
                _lastActiveScreenId = currentId;
                UpdateButtonSelection(currentId);
            }
        }

        // ──────────────────────────────────────────────
        //  导航按钮事件
        // ──────────────────────────────────────────────

        private void OnLobbyNavClicked()
        {
            SetButtonSelected(lobbyNavButton?.gameObject);
            // 关闭所有面板，回到大厅
            while (UIManager.Instance.GetCurrentScreen() != null)
                UIManager.Instance.CloseCurrent();
            _blurActive = false;
            SetBlurAndRotation(false);
        }

        private void OnCharacterNavClicked()
        {
            SetButtonSelected(characterNavButton?.gameObject);
            // CloseCurrentPanel(); // 旧方式：平级切换，先关再开
            UIManager.Instance.Open(CharactorPanelId);
            _lastActiveScreenId = CharactorPanelId;
            ActivateBlur();
        }

        private void OnRuneNavClicked()
        {
            SetButtonSelected(runeNavButton?.gameObject);
            // CloseCurrentPanel(); // 旧方式：平级切换，先关再开
            UIManager.Instance.Open(RunePanelId);
            _lastActiveScreenId = RunePanelId;
            ActivateBlur();
        }

        private void OnMissionNavClicked()
        {
            SetButtonSelected(missionNavButton?.gameObject);
            // CloseCurrentPanel(); // 旧方式：平级切换，先关再开
            UIManager.Instance.Open(MassionPanelId);
            _lastActiveScreenId = MassionPanelId;
            ActivateBlur();
        }

        private void OnShopNavClicked()
        {
            SetButtonSelected(shopNavButton?.gameObject);
            // CloseCurrentPanel(); // 旧方式：平级切换，先关再开
            UIManager.Instance.Open(ShopPanelId);
            _lastActiveScreenId = ShopPanelId;
            ActivateBlur();
        }

        private void OnSettingsNavClicked()
        {
            UIManager.Instance.Open(SettingsPanelId);
            _lastActiveScreenId = SettingsPanelId;
            ActivateBlur();
        }

        // ──────────────────────────────────────────────
        //  辅助方法
        // ──────────────────────────────────────────────

        private void CloseCurrentPanel()
        {
            var current = UIManager.Instance.GetCurrentScreen();
            if (current != null)
                UIManager.Instance.Close(current);
        }

        private void ActivateBlur()
        {
            SetBlurAndRotation(true);
            _blurActive = true;
        }

        /// <summary>根据栈顶面板 ID 更新导航按钮选中状态</summary>
        private void UpdateButtonSelection(string screenId)
        {
            GameObject target = screenId switch
            {
                CharactorPanelId => characterNavButton?.gameObject,
                RunePanelId => runeNavButton?.gameObject,
                MassionPanelId => missionNavButton?.gameObject,
                ShopPanelId => shopNavButton?.gameObject,
                SettingsPanelId => settingsNavButton?.gameObject,
                _ => lobbyNavButton?.gameObject,
            };
            SetButtonSelected(target);
        }

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
            // 设置按钮不参与选中状态切换
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
