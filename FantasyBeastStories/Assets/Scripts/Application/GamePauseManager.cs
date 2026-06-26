using Domain.Event;
using Domain.Event.Channels.Game;
using Domain.Services;
using Photon.Pun; // 仅保留 [PunRPC] 属性引用
using UnityEngine;

namespace Application
{
    public class GamePauseManager : MonoBehaviour
    {
        public static bool isPaused = false;
        public static GamePauseManager instance;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else Destroy(gameObject);
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
            isPaused = pause;
            EventChannelLocator.MainContainer.gameSettings.IsPaused = pause;

            var pauseStateChannel = EventChannelLocator.MainContainer.pauseStateChannel;
            if (pauseStateChannel != null) pauseStateChannel.Raise(pause);

            if (NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                NetworkServiceLocator.PlayerService.SetRoomCustomProperty("IsPaused", isPaused);
                NetworkServiceLocator.ObjectService.InvokeRPC(this, "RPC_SetPauseState", NetworkTarget.All, isPaused);
            }
        }

        [PunRPC]
        public void RPC_SetPauseState(bool pause)
        {
            isPaused = pause;
            EventChannelLocator.MainContainer.gameSettings.IsPaused = pause;
            var pauseStateChannel = EventChannelLocator.MainContainer.pauseStateChannel;
            if (pauseStateChannel != null) pauseStateChannel.Raise(pause);
        }
    }
}