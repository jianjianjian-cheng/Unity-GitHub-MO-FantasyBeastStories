using Core.Channels.General;
using UnityEngine;

namespace Core.Channels.Player
{
    [CreateAssetMenu(menuName = "Events/SubContainers/Player Channels")]
    public class PlayerChannelsSO : ScriptableObject
    {
        public FloatEventChannelSO hpChangedChannel;
        public CardConfigEventChannelSO cardReceivedChannel;
        public PlayerQueryEventChannelSO playerQueryChannel;
        public PlayerAttributeEventChannelSO playerAttributeChannel;
        public ExperienceEventChannelSO experienceChannel;
        public SkillQueryEventChannelSO skillQueryChannel;
    }
}