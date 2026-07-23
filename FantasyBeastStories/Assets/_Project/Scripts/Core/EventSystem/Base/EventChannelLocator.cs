using Managers;
using Core.Channels;
using Core.Channels.Combat;
using Core.Channels.Game;
using Core.Channels.Player;
using Core.Channels.UI;
using UnityEngine;

namespace Core
{
    public static class EventChannelLocator
    {
        private static EventChannelContainerSO _mainContainer;

        public static EventChannelContainerSO MainContainer
        {
            get
            {
                if (_mainContainer == null)
                {
                    _mainContainer = ServiceLocator.Get<EventChannelContainerSO>();

                    if (_mainContainer == null)
                    {
                        _mainContainer = Resources.Load<EventChannelContainerSO>("EventChannels/MainEventChannels");
                    }

                    if (_mainContainer == null)
                    {
                        Debug.LogError("未找到 MainEventChannels 资源，请在 Resources/EventChannels 目录下创建");
                    }
                }
                return _mainContainer;
            }
        }

        // ========== 功能系统子容器便捷访问 ==========
        public static CombatChannelsSO MatchSystem => MainContainer?.matchSystem;
        public static PlayerChannelsSO ProgressionSystem => MainContainer?.progressionSystem;
        public static GameChannelsSO LobbySystem => MainContainer?.lobbySystem;
        public static UIChannelsSO UISystem => MainContainer?.uiSystem;
    }
}