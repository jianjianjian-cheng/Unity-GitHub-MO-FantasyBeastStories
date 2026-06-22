using System;
using Photon.Pun;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 游戏暂停管理器 - 负责游戏暂停/恢复逻辑
    /// </summary>
    public class GamePauseManager : MonoBehaviourPunCallbacks
    {
        private static GamePauseManager _instance;
        public static GamePauseManager Instance => _instance;
        
        public static bool isPaused => _instance != null && _instance._isPaused;
        
        private bool _isPaused = false;
        
        public static float DeltaTime
        {
            get { return isPaused ? 0f : UnityEngine.Time.deltaTime; }
        }

        public static float FixedDeltaTime
        {
            get { return isPaused ? 0f : UnityEngine.Time.fixedDeltaTime; }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                EnsurePhotonView();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            // 不使用DontDestroyOnLoad，保持在场景中
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void TogglePause()
        {
            SetPause(!_isPaused);
        }

        public void SetPause(bool pause)
        {
            if (_isPaused == pause) return;
            
            _isPaused = pause;

            if (PhotonNetwork.IsMasterClient)
            {
                var pauseProps = new ExitGames.Client.Photon.Hashtable
                {
                    ["IsPaused"] = _isPaused
                };
                PhotonNetwork.CurrentRoom?.SetCustomProperties(pauseProps);

                photonView?.RPC(nameof(RPC_SetPauseState), RpcTarget.All, _isPaused);
            }
            
            // 发送事件通知
            Core.Event.EventManager.Instance.Emit(new GamePauseEvent(_isPaused));
        }

        [PunRPC]
        private void RPC_SetPauseState(bool pause)
        {
            if (_isPaused == pause) return;
            
            _isPaused = pause;

            if (_isPaused)
            {
                FreezeAllAnimations();
            }
            else
            {
                ResumeAllAnimations();
            }
            
            // 发送事件通知
            Core.Event.EventManager.Instance.Emit(new GamePauseEvent(_isPaused));
        }

        private void FreezeAllAnimations()
        {
            // 可以通过事件系统通知所有动画组件
            Core.Event.EventManager.Instance.Emit(new GameFreezeEvent());
        }

        private void ResumeAllAnimations()
        {
            // 可以通过事件系统通知所有动画组件
            Core.Event.EventManager.Instance.Emit(new GameResumeEvent());
        }

        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            if (propertiesThatChanged.TryGetValue("IsPaused", out var pauseValue))
            {
                bool pause = (bool)pauseValue;
                if (_isPaused != pause)
                {
                    RPC_SetPauseState(pause);
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

    /// <summary>
    /// 游戏暂停事件
    /// </summary>
    public class GamePauseEvent : Core.Event.GameEventBase
    {
        public bool IsPaused { get; }

        public GamePauseEvent(bool isPaused)
        {
            IsPaused = isPaused;
        }
    }

    /// <summary>
    /// 游戏冻结事件（暂停时触发）
    /// </summary>
    public class GameFreezeEvent : Core.Event.GameEventBase { }

    /// <summary>
    /// 游戏恢复事件（恢复时触发）
    /// </summary>
    public class GameResumeEvent : Core.Event.GameEventBase { }
}
