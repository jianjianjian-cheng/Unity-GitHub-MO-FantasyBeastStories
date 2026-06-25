using UnityEngine;
using Domain.Event.Channels;

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
                    _mainContainer = Resources.Load<EventChannelContainerSO>("EventChannels/MainEventChannels");
                    if (_mainContainer == null)
                    {
                        Debug.LogError("未找到 MainEventChannels 资源，请在 Resources/EventChannels 目录下创建");
                    }
                }
                return _mainContainer;
            }
        }
    }
}