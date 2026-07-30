using Core;
using Core.Channels.General;
using Controllers.Rune;
using UI.Framework.Base;
using UI.Framework.Manager;
using UI.Framework.Panel;
using UnityEngine;
using UnityEngine.UI;
using UI.Rune;

namespace UI.Framework.Widget
{
    /// <summary>
    /// 大厅操作组件：就绪按钮 + 退出房间按钮。
    /// </summary>
    public class LobbyActionWidget : UIWidget
    {
        private const string RunePanelId = "RunePanel";

        [Header("按钮")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button exitRoomButton;

        private bool _isReady;

        protected override void SubscribeEvents()
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);
            if (exitRoomButton != null)
                exitRoomButton.onClick.AddListener(OnExitRoomClicked);
        }

        protected override void UnsubscribeEvents()
        {
            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartClicked);
            if (exitRoomButton != null)
                exitRoomButton.onClick.RemoveListener(OnExitRoomClicked);
        }

        // ──────────────────────────────────────────────
        //  就绪
        // ──────────────────────────────────────────────

        private void OnStartClicked()
        {
            var container = EventChannelLocator.MainContainer;
            if (!container.gameSettings.IsStayLobby || _isReady)
                return;

            // 出发前捕获当前装备的符文
            var runePanel = UIManager.HasInstance
                ? UIManager.Instance.GetScreen(RunePanelId) as RunePanel
                : null;
            if (runePanel != null)
                RuneEquipmentSnapshot.CaptureFrom(runePanel);
            else
                Debug.LogWarning("[LobbyActionWidget] 未找到 RunePanel，无法捕获符文装备数据");

            container.gameActionChannel.Raise(GameActionType.SyncAllPlayers);
            _isReady = true;

            if (startButton != null)
            {
                startButton.interactable = false;
                var btnText = startButton.GetComponentInChildren<Text>();
                if (btnText != null)
                    btnText.text = "已就绪";
            }

            container.gameActionChannel.Raise(GameActionType.SetLocalReady);
        }

        // ──────────────────────────────────────────────
        //  退出房间
        // ──────────────────────────────────────────────

        private void OnExitRoomClicked()
        {
            EventChannelLocator.MainContainer.gameActionChannel.Raise(GameActionType.QuitToMainMenu);
        }
    }
}
