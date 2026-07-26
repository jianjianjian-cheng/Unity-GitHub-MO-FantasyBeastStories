using Core.Channels.Base;
using UnityEngine;

namespace UI.Framework.Event
{
    [CreateAssetMenu(menuName = "Events/UI/Screen Open Event Channel")]
    public class ScreenOpenEventChannelSO : BaseEventChannelSO<string> { }

    [CreateAssetMenu(menuName = "Events/UI/Screen Close Event Channel")]
    public class ScreenCloseEventChannelSO : BaseEventChannelSO<string> { }

    [CreateAssetMenu(menuName = "Events/UI/Screen Transition Event Channel")]
    public class ScreenTransitionEventChannelSO : BaseEventChannelSO<string, string> { }

    [CreateAssetMenu(menuName = "Events/UI/Back Button Event Channel")]
    public class BackButtonEventChannelSO : BaseEventChannelSO { }

    [CreateAssetMenu(menuName = "Events/UI/Toast Message Event Channel")]
    public class ToastMessageEventChannelSO : BaseEventChannelSO<string> { }

    [CreateAssetMenu(menuName = "Events/UI/Dialog Confirm Event Channel")]
    public class DialogConfirmEventChannelSO : BaseEventChannelSO<bool> { }

    [CreateAssetMenu(menuName = "Events/UI/Loading Progress Event Channel")]
    public class LoadingProgressEventChannelSO : BaseEventChannelSO<float> { }

    [CreateAssetMenu(menuName = "Events/UI/Currency Update Event Channel")]
    public class CurrencyUpdateEventChannelSO : BaseEventChannelSO<int> { }
}