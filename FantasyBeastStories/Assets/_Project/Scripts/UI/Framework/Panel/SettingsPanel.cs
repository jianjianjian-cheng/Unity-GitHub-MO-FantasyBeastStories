using Core.Network;
using UI.Framework.Base;
using UI.Framework.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Framework.Panel
{
    /// <summary>
    /// 设置面板：包含音量调节入口、退出大厅、返回按钮。
    /// </summary>
    public class SettingsPanel : UIScreen
    {
        [Header("按钮")]
        [SerializeField] private Button audioSettingsButton;
        [SerializeField] private Button quitRoomButton;
        [SerializeField] private Button returnButton;

        protected override void Awake()
        {
            screenId = "SettingsPanel";
            base.Awake();
            UIManager.Instance.RegisterScreen(this);
        }

        protected override void SubscribeEvents()
        {
            if (audioSettingsButton != null)
                audioSettingsButton.onClick.AddListener(OnAudioSettingsClicked);
            if (quitRoomButton != null)
                quitRoomButton.onClick.AddListener(OnQuitRoomClicked);
            if (returnButton != null)
                returnButton.onClick.AddListener(OnReturnClicked);
        }

        protected override void UnsubscribeEvents()
        {
            if (audioSettingsButton != null)
                audioSettingsButton.onClick.RemoveListener(OnAudioSettingsClicked);
            if (quitRoomButton != null)
                quitRoomButton.onClick.RemoveListener(OnQuitRoomClicked);
            if (returnButton != null)
                returnButton.onClick.RemoveListener(OnReturnClicked);
        }

        private void OnAudioSettingsClicked()
        {
            UIManager.Instance.Open("AudioSettingsPanel");
        }

        private void OnQuitRoomClicked()
        {
            NetworkServiceLocator.GameActionService.QuitToMainMenu();
        }

        private void OnReturnClicked()
        {
            CloseSelf();
        }
    }
}
