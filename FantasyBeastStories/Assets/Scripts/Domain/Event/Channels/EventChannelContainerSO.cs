using Domain.Event.Channels.Combat;
using Domain.Event.Channels.Game;
using Domain.Event.Channels.General;
using Domain.Event.Channels.Player;
using Domain.Event.Channels.Task;
using Domain.Event.Channels.UI;
using UnityEngine;

namespace Domain.Event.Channels
{
  [CreateAssetMenu(menuName = "Events/Event Channel Container")]
  public class EventChannelContainerSO : ScriptableObject
  {
    [Header("子容器（在 Inspector 中将各 Channel 拖入对应子容器）")]
    public CombatChannelsSO combat;
    public PlayerChannelsSO player;
    public GameChannelsSO game;
    public UIChannelsSO ui;

    [Header("全局设置")]
    public GameSettingsSO gameSettings;

    // ========== 向后兼容的直接访问属性 ==========
    // 所有现有代码 EventChannelLocator.MainContainer.xxx 无需修改

    #region 战斗事件
    public DamageEventChannelSO damageEventChannel => combat != null ? combat.damageEventChannel : null;
    public PlayerDamageEventChannelSO playerDamageEventChannel => combat != null ? combat.playerDamageEventChannel : null;
    public EnemyReportEventChannelSO enemyReportChannel => combat != null ? combat.enemyReportChannel : null;
    public BossHPUpdateEventChannelSO bossHPUpdateChannel => combat != null ? combat.bossHPUpdateChannel : null;
    #endregion

    #region 玩家事件
    public FloatEventChannelSO hpChangedChannel => player != null ? player.hpChangedChannel : null;
    public CardConfigEventChannelSO cardReceivedChannel => player != null ? player.cardReceivedChannel : null;
    public PlayerQueryEventChannelSO playerQueryChannel => player != null ? player.playerQueryChannel : null;
    public PlayerAttributeEventChannelSO playerAttributeChannel => player != null ? player.playerAttributeChannel : null;
    public ExperienceEventChannelSO experienceChannel => player != null ? player.experienceChannel : null;
    public SkillQueryEventChannelSO skillQueryChannel => player != null ? player.skillQueryChannel : null;
    #endregion

    #region 游戏事件
    public TimeEventChannelSO timeEventChannel => game != null ? game.timeEventChannel : null;
    public GameStateChangeEventChannelSO gameStateChangeChannel => game != null ? game.gameStateChangeChannel : null;
    public SingleFloatEventChannelSO timeChangeEnemyAttributeChannel => game != null ? game.timeChangeEnemyAttributeChannel : null;
    public GameActionEventChannelSO gameActionChannel => game != null ? game.gameActionChannel : null;
    public DifficultyCoefficientQueryEventChannelSO difficultyCoefficientQueryChannel => game != null ? game.difficultyCoefficientQueryChannel : null;
    public GamePauseStateEventChannelSO pauseStateChannel => game != null ? game.pauseStateChannel : null;
    public RoomJoinedEventChannelSO roomJoinedChannel => game != null ? game.roomJoinedChannel : null;
    public PoolOperationEventChannelSO poolOperationChannel => game != null ? game.poolOperationChannel : null;
    public TimeStartedEventChannelSO timeStartedChannel => game != null ? game.timeStartedChannel : null;
    public TimePausedEventChannelSO timePausedChannel => game != null ? game.timePausedChannel : null;
    public TimeResetEventChannelSO timeResetChannel => game != null ? game.timeResetChannel : null;
    public TimeQueryEventChannelSO timeQueryChannel => game != null ? game.timeQueryChannel : null;
    #endregion

    #region UI事件
    public BoolEventChannelSO changeCanRotateChannel => ui != null ? ui.changeCanRotateChannel : null;
    public BoolEventChannelSO pauseChannel => ui != null ? ui.pauseChannel : null;
    public RuneInfoEventChannelSO runeInfoChannel => ui != null ? ui.runeInfoChannel : null;
    public MagicUpgradeEventChannelSO magicUpgradeChannel => ui != null ? ui.magicUpgradeChannel : null;
    public LoadingEventChannelSO loadingChannel => ui != null ? ui.loadingChannel : null;
    public SingleFloatEventChannelSO bloomChannel => ui != null ? ui.bloomChannel : null;
    public ExperienceUpdateEventChannelSO experienceUpdateChannel => ui != null ? ui.experienceUpdateChannel : null;
    public TaskUIEventChannelSO taskUIChannel => ui != null ? ui.taskUIChannel : null;
    public HealthUpdateEventChannelSO healthUpdateChannel => ui != null ? ui.healthUpdateChannel : null;
    #endregion

    #region 任务事件
    public TaskActivationEventChannelSO taskActivationChannel => ui != null ? ui.taskActivationChannel : null;
    public TaskNoticeEventChannelSO taskNoticeChannel => ui != null ? ui.taskNoticeChannel : null;
    #endregion
  }
}