using Domain.Event.Channels.Combat;
using Domain.Event.Channels.General;
using UnityEngine;

namespace Domain.Event.Channels.Game
{
    [CreateAssetMenu(menuName = "Events/SubContainers/Game Channels")]
    public class GameChannelsSO : ScriptableObject
    {
        [Header("游戏事件")]
        public TimeEventChannelSO timeEventChannel;
        public GameStateChangeEventChannelSO gameStateChangeChannel;
        public SingleFloatEventChannelSO timeChangeEnemyAttributeChannel;
        public GameActionEventChannelSO gameActionChannel;
        public DifficultyCoefficientQueryEventChannelSO difficultyCoefficientQueryChannel;
        public GamePauseStateEventChannelSO pauseStateChannel;
        public RoomJoinedEventChannelSO roomJoinedChannel;

        [Header("对象池事件")]
        public PoolOperationEventChannelSO poolOperationChannel;

        [Header("时间事件")]
        public TimeStartedEventChannelSO timeStartedChannel;
        public TimePausedEventChannelSO timePausedChannel;
        public TimeResetEventChannelSO timeResetChannel;

        [Header("时间查询")]
        public TimeQueryEventChannelSO timeQueryChannel;

        [Header("Boss事件")]
        public BossSpawnEventChannelSO bossSpawnChannel;

        [Header("道具事件")]
        public PowerUpCollectEventChannelSO powerUpCollectChannel;
    }
}