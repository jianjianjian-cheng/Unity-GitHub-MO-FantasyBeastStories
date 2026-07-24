using Core.Channels.Combat;
using Core.Channels.Game;
using Core.Channels.General;
using Core.Channels.Player;
using Core.Channels.Shop;
using Core.Channels.Task;
using Core.Channels.UI;
using UnityEngine;

namespace Core.Channels
{
  [CreateAssetMenu(menuName = "Events/Event Channel Container")]
  public class EventChannelContainerSO : ScriptableObject
  {
    [Header("子容器（在 Inspector 拖进去）")]
    [Tooltip("对局系统：战斗伤害、Boss、敌人报告")]
    public CombatChannelsSO matchSystem;
    [Tooltip("成长系统：玩家属性、经验、卡牌、技能")]
    public PlayerChannelsSO progressionSystem;
    [Tooltip("大厅系统：游戏状态、时间、对象池、房间")]
    public GameChannelsSO lobbySystem;
    [Tooltip("UI系统：界面更新、任务通知、金币显示")]
    public UIChannelsSO uiSystem;

    /// <summary>
    /// 下面是快捷访问的
    /// </summary>
    [Header("全局设置")]
    public GameSettingsSO gameSettings;

    #region 战斗事件
    public DamageEventChannelSO damageEventChannel => matchSystem != null ? matchSystem.damageEventChannel : null;
    public PlayerDamageEventChannelSO playerDamageEventChannel => matchSystem != null ? matchSystem.playerDamageEventChannel : null;
    public EnemyReportEventChannelSO enemyReportChannel => matchSystem != null ? matchSystem.enemyReportChannel : null;
    public BossHPUpdateEventChannelSO bossHPUpdateChannel => matchSystem != null ? matchSystem.bossHPUpdateChannel : null;
    public BossDeathEventChannelSO bossDeathChannel => matchSystem?.bossDeathChannel;
    public BossSpawnEventChannelSO bossSpawnChannel => lobbySystem?.bossSpawnChannel;
    #endregion

    #region 玩家事件
    public FloatEventChannelSO hpChangedChannel => progressionSystem != null ? progressionSystem.hpChangedChannel : null;
    public CardConfigEventChannelSO cardReceivedChannel => progressionSystem != null ? progressionSystem.cardReceivedChannel : null;
    public PlayerQueryEventChannelSO playerQueryChannel => progressionSystem != null ? progressionSystem.playerQueryChannel : null;
    public PlayerAttributeEventChannelSO playerAttributeChannel => progressionSystem != null ? progressionSystem.playerAttributeChannel : null;
    public ExperienceEventChannelSO experienceChannel => progressionSystem != null ? progressionSystem.experienceChannel : null;
    public SkillQueryEventChannelSO skillQueryChannel => progressionSystem != null ? progressionSystem.skillQueryChannel : null;
    #endregion

    #region 游戏事件
    public TimeEventChannelSO timeEventChannel => lobbySystem != null ? lobbySystem.timeEventChannel : null;
    public GameStateChangeEventChannelSO gameStateChangeChannel => lobbySystem != null ? lobbySystem.gameStateChangeChannel : null;
    public SingleFloatEventChannelSO timeChangeEnemyAttributeChannel => lobbySystem != null ? lobbySystem.timeChangeEnemyAttributeChannel : null;
    public GameActionEventChannelSO gameActionChannel => lobbySystem != null ? lobbySystem.gameActionChannel : null;
    public DifficultyCoefficientQueryEventChannelSO difficultyCoefficientQueryChannel => lobbySystem != null ? lobbySystem.difficultyCoefficientQueryChannel : null;
    public GamePauseStateEventChannelSO pauseStateChannel => lobbySystem != null ? lobbySystem.pauseStateChannel : null;
    public RoomJoinedEventChannelSO roomJoinedChannel => lobbySystem != null ? lobbySystem.roomJoinedChannel : null;
    public PoolOperationEventChannelSO poolOperationChannel => lobbySystem != null ? lobbySystem.poolOperationChannel : null;
    public TimeStartedEventChannelSO timeStartedChannel => lobbySystem != null ? lobbySystem.timeStartedChannel : null;
    public TimePausedEventChannelSO timePausedChannel => lobbySystem != null ? lobbySystem.timePausedChannel : null;
    public TimeResetEventChannelSO timeResetChannel => lobbySystem != null ? lobbySystem.timeResetChannel : null;
    public TimeQueryEventChannelSO timeQueryChannel => lobbySystem != null ? lobbySystem.timeQueryChannel : null;
    public PowerUpCollectEventChannelSO powerUpCollectChannel => lobbySystem != null ? lobbySystem.powerUpCollectChannel : null;
    #endregion

    #region UI事件
    public BoolEventChannelSO changeCanRotateChannel => uiSystem != null ? uiSystem.changeCanRotateChannel : null;
    public BoolEventChannelSO pauseChannel => uiSystem != null ? uiSystem.pauseChannel : null;
    public RuneInfoEventChannelSO runeInfoChannel => uiSystem != null ? uiSystem.runeInfoChannel : null;
    public MagicUpgradeEventChannelSO magicUpgradeChannel => uiSystem != null ? uiSystem.magicUpgradeChannel : null;
    public LoadingEventChannelSO loadingChannel => uiSystem != null ? uiSystem.loadingChannel : null;
    public SingleFloatEventChannelSO bloomChannel => uiSystem != null ? uiSystem.bloomChannel : null;
    public ExperienceUpdateEventChannelSO experienceUpdateChannel => uiSystem != null ? uiSystem.experienceUpdateChannel : null;
    public TaskUIEventChannelSO taskUIChannel => uiSystem != null ? uiSystem.taskUIChannel : null;
    public HealthUpdateEventChannelSO healthUpdateChannel => uiSystem != null ? uiSystem.healthUpdateChannel : null;
    public CoinUpdateEventChannelSO coinUpdateChannel => uiSystem != null ? uiSystem.coinUpdateChannel : null;
    public MatchStatsUpdateEventChannelSO matchStatsUpdateChannel => uiSystem != null ? uiSystem.matchStatsUpdateChannel : null;
    public ShopEventChannelSO shopEventChannel => uiSystem != null ? uiSystem.shopEventChannel : null;
    #endregion

    #region 任务事件
    public TaskActivationEventChannelSO taskActivationChannel => uiSystem != null ? uiSystem.taskActivationChannel : null;
    public TaskNoticeEventChannelSO taskNoticeChannel => uiSystem != null ? uiSystem.taskNoticeChannel : null;
    #endregion
  }
}