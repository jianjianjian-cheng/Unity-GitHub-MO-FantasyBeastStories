using Application;
using Domain.Event.Channels;
using Domain.Event.Channels.Combat;
using Domain.Event.Channels.Game;
using Domain.Event.Channels.Player;
using Domain.Event.Channels.UI;
using UnityEngine;

namespace Domain.Event
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

        // ========== 领域子容器便捷访问 ==========
        public static CombatChannelsSO Combat => MainContainer?.combat;
        public static PlayerChannelsSO Player => MainContainer?.player;
        public static GameChannelsSO Game => MainContainer?.game;
        public static UIChannelsSO UI => MainContainer?.ui;
    }
}