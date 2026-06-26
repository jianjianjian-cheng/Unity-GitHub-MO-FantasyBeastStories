using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Domain.Services;
using Domain.Time.TimeSystem;
using Photon.Pun;
using UnityEngine;
using DG.Tweening;
using Domain.Event;
using Domain.Event.Channels.Game;

namespace Domain.Time
{
    /// <summary>
    /// 全局游戏时间管理器（支持Photon PUN2联机同步）
    /// 核心逻辑层：仅处理时间计算、事件触发、网络同步
    /// UI相关代码已剥离至Presentation层
    /// </summary>
    public class SyncedGameTimeManager : MonoBehaviour
    {
        [Header("时间设置")]
        [Tooltip("总游戏时间（秒）")]
        public float totalGameTime = 120f;

        [Tooltip("是否启用网络同步（非主机/房主跟随主机时间）")]
        public bool isSynced = true;

        [Tooltip("是否循环播放（时间结束后重置）")]
        public bool loop = false;

        [Header("事件列表")]
        public List<TimeEventData> timeEvents = new List<TimeEventData>();

        [Header("最终Boss设置")]
        [SerializeField]
        private string bossName = "Boss_Horror";
        private bool isBossGenerated = false;

        // 运行时数据
        private float currentTime = 0f;
        private float lastTriggerTime = 0f;
        private bool isRunning = false;
        private HashSet<string> triggeredEventIds = new HashSet<string>();

        // 事件触发委托
        public Action<TimeEventData> OnTimeEventTriggered;
        public Action<float> OnTimeUpdated;
        public Action OnGameTimeFinished;
        public Action OnGameTimeLoop;

        // 单例
        public static SyncedGameTimeManager Instance { get; private set; }

        private float originDifficultyCoefficient = 1f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            if (NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                NetworkServiceLocator.ObjectService.InvokeRPC(this, "RPC_SyncStartCaTime", NetworkTarget.All);
            }
        }

        //通知其他玩家游戏开始计时
        [PunRPC]
        private void RPC_SyncStartCaTime()
        {
            StartGameTime();
        }

        void Update()
        {
            if (!isRunning)
                return;

            var playerService = NetworkServiceLocator.PlayerService;
            var objectService = NetworkServiceLocator.ObjectService;

            // 时间更新逻辑
            float deltaTime = UnityEngine.Time.deltaTime;

            // 如果不是主机且启用了同步，时间由网络同步驱动，不本地累加
            if (isSynced && playerService.IsConnectedAndInRoom && !playerService.IsMasterClient)
            {
                return;
            }

            // 主机或非同步模式：本地累加时间
            currentTime += deltaTime;

            if (currentTime >= totalGameTime)
            {
                if (loop)
                {
                    currentTime = 0f;
                    triggeredEventIds.Clear();
                    foreach (var evt in timeEvents)
                    {
                        evt.isTriggered = false;
                    }

                    OnGameTimeLoop?.Invoke();

                    if (playerService.IsConnectedAndInRoom && playerService.IsMasterClient && isSynced)
                    {
                        objectService.InvokeRPC(this, "RPC_SyncResetTime", NetworkTarget.All);
                    }
                }
                else
                {
                    currentTime = totalGameTime;
                    isRunning = false;
                    OnGameTimeFinished?.Invoke();

                    if (playerService.IsConnectedAndInRoom && playerService.IsMasterClient && isSynced)
                    {
                        objectService.InvokeRPC(this, "RPC_GameTimeFinished", NetworkTarget.All);
                    }
                }
            }

            // 检查事件触发（仅主机检查并广播）
            if (!playerService.IsConnectedAndInRoom || playerService.IsMasterClient || !isSynced)
            {
                CheckAndTriggerEvents();
            }

            // ✅ 每隔60秒触发一次
            if (currentTime - lastTriggerTime >= 60f)
            {
                lastTriggerTime = currentTime;
                EventChannelLocator.MainContainer.timeChangeEnemyAttributeChannel.Raise(currentTime);
            }

            // 生成最终Boss
            if (currentTime >= totalGameTime - 480f)
            {
                GenerateFinalBoss(bossName);
            }

            // 触发时间更新事件
            OnTimeUpdated?.Invoke(currentTime);

            // 通过SO事件通道广播时间更新
            var timeArgs = new TimeEventArgs(null, currentTime);
            EventChannelLocator.MainContainer.timeEventChannel.Raise(timeArgs);
        }

        void CheckAndTriggerEvents()
        {
            var playerService = NetworkServiceLocator.PlayerService;
            var objectService = NetworkServiceLocator.ObjectService;

            foreach (var timeEvent in timeEvents)
            {
                if (
                    currentTime >= timeEvent.triggerTime
                    && !triggeredEventIds.Contains(timeEvent.eventId)
                )
                {
                    if (timeEvent.once && timeEvent.isTriggered)
                        continue;

                    TriggerEvent(timeEvent);

                    if (timeEvent.once)
                    {
                        timeEvent.isTriggered = true;
                        triggeredEventIds.Add(timeEvent.eventId);

                        if (playerService.IsConnectedAndInRoom && playerService.IsMasterClient && isSynced)
                        {
                            objectService.InvokeRPC(
                                this,
                                "RPC_OnTimeEventTriggered",
                                NetworkTarget.All,
                                timeEvent.eventId,
                                currentTime
                            );
                        }
                    }
                }
            }
        }

        void TriggerEvent(TimeEventData timeEvent)
        {
            Debug.Log(
                $"[TimeManager] 事件触发: {timeEvent.eventName} 时间: {FormatTime(currentTime)}"
            );

            OnTimeEventTriggered?.Invoke(timeEvent);

            var args = new TimeEventArgs
            {
                eventData = timeEvent,
                currentTime = currentTime,
                isFromNetwork = false,
            };
            EventChannelLocator.MainContainer.timeEventChannel.Raise(args);
        }

        #region PUN2 网络同步

        [PunRPC]
        void RPC_OnTimeEventTriggered(string eventId, float triggerTime, PhotonMessageInfo info)
        {
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                var timeEvent = timeEvents.Find(e => e.eventId == eventId);
                if (timeEvent != null && !timeEvent.isTriggered)
                {
                    timeEvent.isTriggered = true;
                    triggeredEventIds.Add(eventId);
                    TriggerEvent(timeEvent);
                }
            }
        }

        [PunRPC]
        void RPC_GameTimeFinished(PhotonMessageInfo info)
        {
            isRunning = false;
            OnGameTimeFinished?.Invoke();
            EventChannelLocator.MainContainer.gameStateChangeChannel.Raise(GameState.GameOver);
        }

        [PunRPC]
        void RPC_SyncSetTime(float time, PhotonMessageInfo info)
        {
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                currentTime = Mathf.Clamp(time, 0, totalGameTime);
            }
        }

        [PunRPC]
        void RPC_SyncStartTime(PhotonMessageInfo info)
        {
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                isRunning = true;
                EventChannelLocator.MainContainer.timeStartedChannel.Raise();
            }
        }

        [PunRPC]
        void RPC_SyncPauseTime(PhotonMessageInfo info)
        {
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                isRunning = false;
                EventChannelLocator.MainContainer.timePausedChannel.Raise();
            }
        }

        [PunRPC]
        void RPC_SyncResetTime(PhotonMessageInfo info)
        {
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                currentTime = 0f;
                triggeredEventIds.Clear();
                foreach (var evt in timeEvents)
                {
                    evt.isTriggered = false;
                }
                EventChannelLocator.MainContainer.timeResetChannel.Raise();
            }
        }

        [PunRPC]
        void RPC_SyncBossUI(float bossHealth)
        {

        }

        public void InitializeBossUI(float maxHealth, string bossName)
        {
            var bossHPUI = GameObject.Find("BossHPUI");
            if (bossHPUI != null)
            {
                bossHPUI.SetActive(true);
                var slider = bossHPUI.GetComponentInChildren<UnityEngine.UI.Slider>();
                if (slider != null)
                {
                    slider.maxValue = maxHealth;
                    slider.value = maxHealth;
                }
                var nameText = bossHPUI.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (nameText != null)
                {
                    nameText.text = bossName;
                }
            }
        }

        public void UpdateHPUI(float currentHealth)
        {
            var bossHPUI = GameObject.Find("BossHPUI");
            if (bossHPUI != null)
            {
                var slider = bossHPUI.GetComponentInChildren<UnityEngine.UI.Slider>();
                if (slider != null)
                {
                    slider.value = currentHealth;
                }
            }
        }
        #endregion

        #region 有关于与时间相关的游戏机制
        private void GenerateFinalBoss(string name)
        {
            if (isBossGenerated) return;
            if (!NetworkServiceLocator.PlayerService.IsMasterClient) return;
            isBossGenerated = true;
        }
        #endregion

        #region 公共控制方法

        public void StartGameTime()
        {
            var playerService = NetworkServiceLocator.PlayerService;
            var objectService = NetworkServiceLocator.ObjectService;

            if (!playerService.IsConnectedAndInRoom || playerService.IsMasterClient || !isSynced)
            {
                isRunning = true;
                if (playerService.IsConnectedAndInRoom && playerService.IsMasterClient && isSynced)
                {
                    objectService.InvokeRPC(this, "RPC_SyncStartTime", NetworkTarget.Others);
                }
                EventChannelLocator.MainContainer.timeStartedChannel.Raise();
            }
        }

        public void PauseGameTime()
        {
            var playerService = NetworkServiceLocator.PlayerService;
            var objectService = NetworkServiceLocator.ObjectService;

            if (!playerService.IsConnectedAndInRoom || playerService.IsMasterClient || !isSynced)
            {
                isRunning = false;
                if (playerService.IsConnectedAndInRoom && playerService.IsMasterClient && isSynced)
                {
                    objectService.InvokeRPC(this, "RPC_SyncPauseTime", NetworkTarget.Others);
                }
                EventChannelLocator.MainContainer.timePausedChannel.Raise();
            }
        }

        public void ResumeGameTime() => StartGameTime();

        public void ResetGameTime()
        {
            var playerService = NetworkServiceLocator.PlayerService;
            var objectService = NetworkServiceLocator.ObjectService;

            if (!playerService.IsConnectedAndInRoom || playerService.IsMasterClient || !isSynced)
            {
                currentTime = 0f;
                triggeredEventIds.Clear();
                foreach (var evt in timeEvents)
                {
                    evt.isTriggered = false;
                }

                if (playerService.IsConnectedAndInRoom && playerService.IsMasterClient && isSynced)
                {
                    objectService.InvokeRPC(this, "RPC_SyncResetTime", NetworkTarget.All);
                }
                EventChannelLocator.MainContainer.timeResetChannel.Raise();
            }
        }

        public void SetTime(float time)
        {
            var playerService = NetworkServiceLocator.PlayerService;
            var objectService = NetworkServiceLocator.ObjectService;

            time = Mathf.Clamp(time, 0f, totalGameTime);
            if (!playerService.IsConnectedAndInRoom || playerService.IsMasterClient || !isSynced)
            {
                currentTime = time;
                if (playerService.IsConnectedAndInRoom && playerService.IsMasterClient && isSynced)
                {
                    objectService.InvokeRPC(this, "RPC_SyncSetTime", NetworkTarget.Others, time);
                }
            }
        }

        public void AddTimeEvent(TimeEventData timeEvent)
        {
            timeEvents.Add(timeEvent);
        }

        public void RemoveTimeEvent(string eventId)
        {
            var evt = timeEvents.Find(e => e.eventId == eventId);
            if (evt != null)
            {
                timeEvents.Remove(evt);
            }
        }

        public float GetCurrentTime() => currentTime;

        public float GetNormalizedTime() => currentTime / totalGameTime;

        public bool IsTimeRunning() => isRunning;

        public float GetRemainingTime() => Mathf.Max(0, totalGameTime - currentTime);

        public string FormatTime(float seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            if (totalGameTime >= 3600)
                return string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
            else
                return string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
        }

        public float GetTotalGameTime() => totalGameTime;

        public bool GetIsGenerated() => isBossGenerated;
        #endregion
    }
}