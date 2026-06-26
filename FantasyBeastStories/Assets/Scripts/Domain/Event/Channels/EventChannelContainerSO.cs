using UnityEngine;
using Domain.Event.Channels.Combat;
using Domain.Event.Channels.Player;
using Domain.Event.Channels.Game;
using Domain.Event.Channels.General;
using Domain.Event.Channels.UI;
using Domain.Event.Channels.Task;

namespace Domain.Event.Channels
{
  [CreateAssetMenu(menuName = "Events/Event Channel Container")]
  public class EventChannelContainerSO : ScriptableObject
  {
    [Header("战斗事件")]
    public DamageEventChannelSO damageEventChannel;
    public EnemyDeathEventChannelSO enemyDeathEventChannel;
    public PlayerDamageEventChannelSO playerDamageEventChannel;
    public EnemySpawnEventChannelSO enemySpawnChannel;
    public EnemyReportEventChannelSO enemyReportChannel;

    [Header("玩家事件")]
    public PlayerAttributeChangeEventChannelSO playerAttributeChangeChannel;
    public PlayerLevelUpEventChannelSO playerLevelUpChannel;
    public FloatEventChannelSO hpChangedChannel;
    public CardConfigEventChannelSO cardReceivedChannel;
    public PlayerQueryEventChannelSO playerQueryChannel;
    public PlayerAttributeEventChannelSO playerAttributeChannel;
    public PlayerHPEventChannelSO playerHPChannel;
    public ExperienceEventChannelSO experienceChannel;
    public SkillQueryEventChannelSO skillQueryChannel;

    [Header("游戏事件")]
    public TimeEventChannelSO timeEventChannel;
    public GameStateChangeEventChannelSO gameStateChangeChannel;
    public SingleFloatEventChannelSO timeChangeEnemyAttributeChannel;
    public GameActionEventChannelSO gameActionChannel;
    public DifficultyCoefficientQueryEventChannelSO difficultyCoefficientQueryChannel;
    public GamePauseStateEventChannelSO pauseStateChannel;
    public RoomJoinedEventChannelSO roomJoinedChannel;

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

    [Header("对象池事件")]
    public PoolOperationEventChannelSO poolOperationChannel;

    [Header("时间事件")]
    public TimeStartedEventChannelSO timeStartedChannel;
    public TimePausedEventChannelSO timePausedChannel;
    public TimeResetEventChannelSO timeResetChannel;

    [Header("全局设置")]
    public GameSettingsSO gameSettings;
  }
}