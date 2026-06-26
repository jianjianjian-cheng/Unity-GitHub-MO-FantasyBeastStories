using Domain.Event.Channels.General;
using Domain.Event.Channels.Task;
using UnityEngine;

namespace Domain.Event.Channels.UI
{
    [CreateAssetMenu(menuName = "Events/SubContainers/UI Channels")]
    public class UIChannelsSO : ScriptableObject
    {
        [Header("UI事件")]
        public BoolEventChannelSO changeCanRotateChannel;
        public BoolEventChannelSO pauseChannel;
        public RuneInfoEventChannelSO runeInfoChannel;
        public MagicUpgradeEventChannelSO magicUpgradeChannel;
        public LoadingEventChannelSO loadingChannel;
        public SingleFloatEventChannelSO bloomChannel;
        public ExperienceUpdateEventChannelSO experienceUpdateChannel;
        public TaskUIEventChannelSO taskUIChannel;
        public HealthUpdateEventChannelSO healthUpdateChannel;

        [Header("任务事件")]
        public TaskActivationEventChannelSO taskActivationChannel;
        public TaskNoticeEventChannelSO taskNoticeChannel;
    }
}