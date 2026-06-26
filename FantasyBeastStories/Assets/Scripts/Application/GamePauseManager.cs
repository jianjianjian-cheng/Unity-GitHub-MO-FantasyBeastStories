using Domain.Event;
using Domain.Event.Channels.Game;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Application
{
    public class GamePauseManager : MonoBehaviourPunCallbacks
    {
        public static bool isPaused = false;
        public static GamePauseManager instance;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                EnsurePhotonView();
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

            if (PhotonNetwork.IsMasterClient)
            {
                Hashtable pauseProps = new Hashtable();
                pauseProps["IsPaused"] = isPaused;
                PhotonNetwork.CurrentRoom.SetCustomProperties(pauseProps);
                if (photonView != null)
                    photonView.RPC("RPC_SetPauseState", RpcTarget.All, isPaused);
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

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            if (propertiesThatChanged.ContainsKey("IsPaused"))
            {
                bool newPauseState = (bool)propertiesThatChanged["IsPaused"];
                if (isPaused != newPauseState)
                {
                    isPaused = newPauseState;
                    EventChannelLocator.MainContainer.gameSettings.IsPaused = newPauseState;
                    var pauseStateChannel = EventChannelLocator.MainContainer.pauseStateChannel;
                    if (pauseStateChannel != null) pauseStateChannel.Raise(newPauseState);
                }
            }
        }

        private void EnsurePhotonView()
        {
            PhotonView pv = GetComponent<PhotonView>();
            if (pv == null) pv = gameObject.AddComponent<PhotonView>();
        }
    }
}