using System;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Framework
{
  /// <summary>
  /// 游戏流程状态
  /// </summary>
  public enum GameFlowState
  {
    None,
    MainMenu,
    Lobby,
    Loading,
    Playing,
    Paused,
    GameOver,
  }

  /// <summary>
  /// 游戏流程管理器接口
  /// </summary>
  public interface IGameFlowManager
  {
    GameFlowState CurrentState { get; }
    bool IsInLobby { get; }
    bool IsTestMode { get; }
    bool IsReady { get; }

    void SetState(GameFlowState newState);
    void SetReady(bool ready);
    void QuitToMainMenu();
    void LoadScene(int sceneIndex);
    void SetLobbyMode(bool isLobby);
    void SetTestMode(bool isTest);
  }

  /// <summary>
  /// 游戏流程管理器 - 负责游戏整体流程控制
  /// 分离自GameManager
  /// </summary>
  public class GameFlowManager : Core.MonoSingleton<GameFlowManager>, IGameFlowManager
  {
    private GameFlowState _currentState = GameFlowState.None;

    public GameFlowState CurrentState => _currentState;

    // 配置
    [SerializeField] private bool _isStayLobbyInspector = true;
    [SerializeField] private bool _isTestInspector = false;
    [SerializeField] private int _startSceneIndex = 2;

    // 静态访问属性（兼容旧代码）
    public static bool isTest => Instance != null ? Instance._isTestMode : false;
    public static bool isStayLobby => Instance != null ? Instance._isLobbyMode : true;
    public static bool isReady => Instance != null ? Instance.IsReady : false;
    public static GameFlowManager instance => Instance;

    public bool IsInLobby => _isLobbyMode;
    public bool IsTestMode => _isTestMode;
    public bool IsReady => _isReady;

    private bool _isReady = false;
    private bool _isLobbyMode = true;
    private bool _isTestMode = false;

    protected override void Awake()
    {
      base.Awake();

      _isLobbyMode = _isStayLobbyInspector;
      _isTestMode = _isTestInspector;

      SetState(GameFlowState.Lobby);
    }

    public void SetState(GameFlowState newState)
    {
      if (_currentState == newState) return;

      var oldState = _currentState;
      _currentState = newState;

      Debug.Log($"[GameFlowManager] State changed: {oldState} -> {newState}");

      // 通知状态变化
      Core.Event.EventManager.Instance.Emit(new GameFlowStateChangedEvent(oldState, newState));
    }

    public void SetReady(bool ready)
    {
      _isReady = ready;
      Core.Event.EventManager.Instance.Emit(new GameReadyStateChangedEvent(ready));
    }

    public void SetLobbyMode(bool isLobby)
    {
      _isLobbyMode = isLobby;
    }

    public void SetTestMode(bool isTest)
    {
      _isTestMode = isTest;
    }

    public void QuitToMainMenu()
    {
      SetReady(false);
      SetState(GameFlowState.MainMenu);
      SceneManager.LoadScene(0);
    }

    public void LoadScene(int sceneIndex)
    {
      SetState(GameFlowState.Loading);
      SceneLoader.Instance.LoadSceneAsync(sceneIndex, () =>
      {
        SetState(_isLobbyMode ? GameFlowState.Lobby : GameFlowState.Playing);
      });
    }

    public void StartGame()
    {
      if (!_isLobbyMode || _isReady)
      {
        return;
      }

      SetReady(true);
    }

    // 兼容旧代码的静态方法
    public static void QuitToMainMenuStatic()
    {
      Instance?.QuitToMainMenu();
    }
  }

  /// <summary>
  /// 游戏流程状态变化事件
  /// </summary>
  public class GameFlowStateChangedEvent : Core.Event.GameEventBase
  {
    public GameFlowState OldState { get; }
    public GameFlowState NewState { get; }

    public GameFlowStateChangedEvent(GameFlowState oldState, GameFlowState newState)
    {
      OldState = oldState;
      NewState = newState;
    }
  }

  /// <summary>
  /// 玩家准备状态变化事件
  /// </summary>
  public class GameReadyStateChangedEvent : Core.Event.GameEventBase
  {
    public bool IsReady { get; }

    public GameReadyStateChangedEvent(bool isReady)
    {
      IsReady = isReady;
    }
  }

  /// <summary>
  /// 场景加载开始事件
  /// </summary>
  public class SceneLoadStartedEvent : Core.Event.GameEventBase
  {
    public int SceneIndex { get; }
    public string SceneName { get; }

    public SceneLoadStartedEvent(int sceneIndex, string sceneName)
    {
      SceneIndex = sceneIndex;
      SceneName = sceneName;
    }
  }

  /// <summary>
  /// 场景加载完成事件
  /// </summary>
  public class SceneLoadCompletedEvent : Core.Event.GameEventBase
  {
    public int SceneIndex { get; }
    public string SceneName { get; }

    public SceneLoadCompletedEvent(int sceneIndex, string sceneName)
    {
      SceneIndex = sceneIndex;
      SceneName = sceneName;
    }
  }
}
