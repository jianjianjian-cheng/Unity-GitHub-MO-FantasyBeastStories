using Core.Channels.General;
using Core.Channels.Task;
using UnityEngine;

namespace Core.Channels.UI
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

        [Header("金币事件")]
        public CoinUpdateEventChannelSO coinUpdateChannel;

        [Header("对局统计事件")]
        public MatchStatsUpdateEventChannelSO matchStatsUpdateChannel;

        [Header("任务事件")]
        public TaskActivationEventChannelSO taskActivationChannel;
        public TaskNoticeEventChannelSO taskNoticeChannel;
    }
}