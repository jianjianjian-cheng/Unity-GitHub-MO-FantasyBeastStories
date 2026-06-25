using System.Collections;
using System.Collections.Generic;
using Domain.Event;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.AI;

namespace Domain.Manager
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
            else
            {
                Destroy(gameObject);
            }
        }

        void OnEnable()
        {
            EventChannelLocator.MainContainer.pauseChannel.RegisterListener(SetPause);
        }

        void OnDisable()
        {
            EventChannelLocator.MainContainer.pauseChannel.UnregisterListener(SetPause);
        }

        public static float DeltaTime
        {
            get { return isPaused ? 0f : UnityEngine.Time.deltaTime; }
        }

        public static float FixedDeltaTime
        {
            get { return isPaused ? 0f : UnityEngine.Time.fixedDeltaTime; }
        }

        void Update()
        {
        }

        public void TogglePause()
        {
            SetPause(!isPaused);
        }

        public void SetPause(bool pause)
        {
            isPaused = pause;
            EventChannelLocator.MainContainer.gameSettings.IsPaused = pause;

            if (PhotonNetwork.IsMasterClient)
            {
                ExitGames.Client.Photon.Hashtable pauseProps = new ExitGames.Client.Photon.Hashtable();
                pauseProps["IsPaused"] = isPaused;
                PhotonNetwork.CurrentRoom.SetCustomProperties(pauseProps);

                if (photonView != null)
                {
                    photonView.RPC("RPC_SetPauseState", RpcTarget.All, isPaused);
                }
            }
        }

        [PunRPC]
        public void RPC_SetPauseState(bool pause)
        {
            isPaused = pause;
            EventChannelLocator.MainContainer.gameSettings.IsPaused = pause;

            if (pause)
            {
                FreezeAllAnimations();
                FreezeAllMovement();
            }
            else
            {
                ResumeAllAnimations();
                ResumeAllMovement();
            }
        }

        private void FreezeAllAnimations()
        {
            Animator[] allAnimators = FindObjectsOfType<Animator>();
            foreach (Animator animator in allAnimators)
            {
                animator.speed = 0f;
                animator.Update(0f);
            }
        }

        private void ResumeAllAnimations()
        {
            Animator[] allAnimators = FindObjectsOfType<Animator>();
            foreach (Animator animator in allAnimators)
            {
                animator.speed = 1f;
            }
        }

        private void FreezeAllMovement()
        {
            NavMeshAgent[] allAgents = FindObjectsOfType<NavMeshAgent>();
            foreach (NavMeshAgent agent in allAgents)
            {
                agent.isStopped = true;
            }

            Rigidbody[] allRigidbodies = FindObjectsOfType<Rigidbody>();
            foreach (Rigidbody rb in allRigidbodies)
            {
                rb.isKinematic = true;
            }
        }

        private void ResumeAllMovement()
        {
            NavMeshAgent[] allAgents = FindObjectsOfType<NavMeshAgent>();
            foreach (NavMeshAgent agent in allAgents)
            {
                agent.isStopped = false;
            }

            Rigidbody[] allRigidbodies = FindObjectsOfType<Rigidbody>();
            foreach (Rigidbody rb in allRigidbodies)
            {
                rb.isKinematic = false;
            }
        }

        public override void OnRoomPropertiesUpdate(
            ExitGames.Client.Photon.Hashtable propertiesThatChanged
        )
        {
            if (propertiesThatChanged.ContainsKey("IsPaused"))
            {
                bool newPauseState = (bool)propertiesThatChanged["IsPaused"];
                if (isPaused != newPauseState)
                {
                    isPaused = newPauseState;
                    EventChannelLocator.MainContainer.gameSettings.IsPaused = newPauseState;
                    if (isPaused)
                    {
                        FreezeAllAnimations();
                        FreezeAllMovement();
                    }
                    else
                    {
                        ResumeAllAnimations();
                        ResumeAllMovement();
                    }
                }
            }
        }

        private void EnsurePhotonView()
        {
            PhotonView pv = GetComponent<PhotonView>();
            if (pv == null)
            {
                pv = gameObject.AddComponent<PhotonView>();
            }
        }
    }
}