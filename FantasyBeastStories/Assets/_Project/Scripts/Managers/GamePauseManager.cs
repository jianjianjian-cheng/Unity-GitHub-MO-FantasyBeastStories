using Core;
using Core.Channels.Game;
using Controllers.Services;
using NetworkTarget = Controllers.Network.NetworkTarget;
using Controllers.Network;
using UnityEngine;

namespace Managers
{
  public class GamePauseManager : MonoBehaviour
  {
    public static bool isPaused = false;

    void Awake()
    {
      ServiceLocator.Register(this);
    }

    void OnEnable()
    {
      EventChannelLocator.MainContainer.pauseChannel.RegisterListener(SetPause);
    }

    void OnDisable()
    {
      EventChannelLocator.MainContainer.pauseChannel.UnregisterListener(SetPause);
    }

    public static float DeltaTime => isPaused ? 0f : UnityEngine.Time.deltaTime;
    public static float FixedDeltaTime => isPaused ? 0f : UnityEngine.Time.fixedDeltaTime;

    public void TogglePause() => SetPause(!isPaused);

    public void SetPause(bool pause)
    {
      HandlePauseStateRPC(pause);

      if (NetworkServiceLocator.PlayerService.IsMasterClient)
      {
        NetworkServiceLocator.PlayerService.SetRoomCustomProperty("IsPaused", isPaused);
        NetworkServiceLocator.ObjectService.InvokeRPC(ManagerRpcBridge.Instance, "RPC_SetPauseState", NetworkTarget.All, isPaused);
      }
    }

    public static void HandlePauseStateRPC(bool pause)
    {
      isPaused = pause;
      var pauseStateChannel = EventChannelLocator.MainContainer.pauseStateChannel;
      if (pauseStateChannel != null) pauseStateChannel.Raise(pause);
    }
  }
}