using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Application;
using Domain.Services;
using Domain.Time.TimeSystem;
using UnityEngine;
using DG.Tweening;
using Domain.Event;
using Domain.Event.Channels.Game;
using Photon.Pun;

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
        public float totalGameTime = 1800f;

        [Tooltip("是否启用网络同步（非主机/房主跟随主机时间）")]
        public bool isSynced = true;

        [Tooltip("是否循环播放（时间结束后重置）")]
        public bool loop = false;

        [Header("事件列表（数据驱动）")]
        [SerializeField]
        private TimeEventListSO timeEventList;

        /// <summary>运行时事件列表（从 SO 加载副本，支持运行时增删）</summary>
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

        private float originDifficultyCoefficient = 1f;

        /// <summary>
        /// 单例实例引用（供静态Handler方法使用）
        /// </summary>
        public static SyncedGameTimeManager Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            ServiceLocator.Register(this);
            DomainServiceLocator.Register(this);

            // 从 ScriptableObject 加载事件列表副本
            if (timeEventList != null)
            {
                timeEvents = timeEventList.GetEvents();
                Debug.Log($"[SyncedGameTimeManager] 从 {timeEventList.name} 加载了 {timeEvents.Count} 个时间事件");
            }
            else
            {
                Debug.LogWarning("[SyncedGameTimeManager] timeEventList 未赋值，使用空的运行时列表");
                timeEvents = new List<TimeEventData>();
            }
        }

        void Start()
        {
            if (NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                NetworkServiceLocator.DomainRpcService?.InvokeRPC("RPC_SyncStartCaTime", NetworkTarget.All);
            }
        }

        void OnEnable()
        {
            if (EventChannelLocator.MainContainer.timeQueryChannel != null)
            {
                EventChannelLocator.MainContainer.timeQueryChannel.RegisterListener(OnTimeQuery);
            }
            if (EventChannelLocator.MainContainer.pauseChannel != null)
            {
                EventChannelLocator.MainContainer.pauseChannel.RegisterListener(OnGamePauseChanged);
            }
        }

        void OnDisable()
        {
            if (EventChannelLocator.MainContainer.timeQueryChannel != null)
            {
                EventChannelLocator.MainContainer.timeQueryChannel.UnregisterListener(OnTimeQuery);
            }
            if (EventChannelLocator.MainContainer.pauseChannel != null)
            {
                EventChannelLocator.MainContainer.pauseChannel.UnregisterListener(OnGamePauseChanged);
            }
        }

        /// <summary>
        /// 响应 TimeQueryEventChannelSO 的查询请求
        /// </summary>
        private void OnTimeQuery(TimeQueryData data)
        {
            data.currentTime = currentTime;
            data.normalizedTime = GetNormalizedTime();
            data.totalGameTime = totalGameTime;
            data.remainingTime = GetRemainingTime();
            data.isTimeRunning = isRunning;
        }

        /// <summary>
        /// 响应 pauseChannel — 选择卡片/升级面板打开时暂停计时
        /// </summary>
        private void OnGamePauseChanged(bool isPaused)
        {
            isRunning = !isPaused;
        }

        // ---- 静态 Handler 方法（供 DomainRpcBridge 调用） ----

        /// <summary>
        /// 由 DomainRpcBridge.RPC_SyncStartCaTime 调用
        /// </summary>
        public static void HandleSyncStartCaTime()
        {
            if (Instance != null) Instance.StartGameTime();
        }

        /// <summary>
        /// 由 DomainRpcBridge.RPC_OnTimeEventTriggered 调用
        /// </summary>
        public static void HandleOnTimeEventTriggered(string eventId, float triggerTime)
        {
            if (Instance == null) return;
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                var timeEvent = Instance.timeEvents.Find(e => e.eventId == eventId);
                if (timeEvent != null && !timeEvent.isTriggered)
                {
                    timeEvent.isTriggered = true;
                    Instance.triggeredEventIds.Add(eventId);
                    Instance.TriggerEvent(timeEvent);
                }
            }
        }

        /// <summary>
        /// 由 DomainRpcBridge.RPC_GameTimeFinished 调用
        /// </summary>
        public static void HandleGameTimeFinished()
        {
            if (Instance == null) return;
            Instance.isRunning = false;
            Instance.OnGameTimeFinished?.Invoke();
            EventChannelLocator.MainContainer.gameStateChangeChannel.Raise(GameState.GameOver);
        }

        /// <summary>
        /// 由 DomainRpcBridge.RPC_BossSpawn 调用
        /// 非主机客户端收到通知后，触发本地事件通道 → UI 更新
        /// </summary>
        public static void HandleBossSpawn(string bossName)
        {
            if (Instance == null) return;
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                Instance.isBossGenerated = true;
                EventChannelLocator.MainContainer.bossSpawnChannel.Raise(bossName);
            }
        }

        /// <summary>
        /// 由 DomainRpcBridge.RPC_SyncSetTime 调用
        /// </summary>
        public static void HandleSyncSetTime(float time)
        {
            if (Instance == null) return;
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                Instance.currentTime = Mathf.Clamp(time, 0, Instance.totalGameTime);
            }
        }

        /// <summary>
        /// 由 DomainRpcBridge.RPC_SyncStartTime 调用
        /// </summary>
        public static void HandleSyncStartTime()
        {
            if (Instance == null) return;
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                Instance.isRunning = true;
                EventChannelLocator.MainContainer.timeStartedChannel.Raise();
            }
        }

        /// <summary>
        /// 由 DomainRpcBridge.RPC_SyncPauseTime 调用
        /// </summary>
        public static void HandleSyncPauseTime()
        {
            if (Instance == null) return;
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                Instance.isRunning = false;
                EventChannelLocator.MainContainer.timePausedChannel.Raise();
            }
        }

        /// <summary>
        /// 由 DomainRpcBridge.RPC_SyncResetTime 调用
        /// </summary>
        public static void HandleSyncResetTime()
        {
            if (Instance == null) return;
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                Instance.currentTime = 0f;
                Instance.triggeredEventIds.Clear();
                foreach (var evt in Instance.timeEvents)
                {
                    evt.isTriggered = false;
                }
                EventChannelLocator.MainContainer.timeResetChannel.Raise();
            }
        }

        void Update()
        {
            if (!isRunning)
                return;

            var playerService = NetworkServiceLocator.PlayerService;

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
                        NetworkServiceLocator.DomainRpcService?.InvokeRPC("RPC_SyncResetTime", NetworkTarget.All);
                    }
                }
                else
                {
                    currentTime = totalGameTime;
                    isRunning = false;
                    OnGameTimeFinished?.Invoke();

                    if (playerService.IsConnectedAndInRoom && playerService.IsMasterClient && isSynced)
                    {
                        NetworkServiceLocator.DomainRpcService?.InvokeRPC("RPC_GameTimeFinished", NetworkTarget.All);
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

            // 生成最终Boss（仅主机检测时间条件）
            if (currentTime >= totalGameTime - 900f && !isBossGenerated)
            {
                isBossGenerated = true;

                // 所有客户端：触发事件通道 → BossSpawner 生成 + UI 更新
                EventChannelLocator.MainContainer.bossSpawnChannel.Raise(bossName);

                // 联机同步：通知非主机客户端触发本地事件（用于 UI 等表现层）
                if (NetworkServiceLocator.PlayerService.IsMasterClient && isSynced)
                {
                    NetworkServiceLocator.DomainRpcService?.InvokeRPC(
                        "RPC_BossSpawn", NetworkTarget.Others, bossName);
                }
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
                            NetworkServiceLocator.DomainRpcService?.InvokeRPC(
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

        // GenerateFinalBoss 逻辑已迁移至 Update 中的事件通道方式
        // 由 bossSpawnChannel.Raise(bossName) 触发，BossSpawner 监听处理

        #region 公共控制方法

        public void StartGameTime()
        {
            var playerService = NetworkServiceLocator.PlayerService;

            if (!playerService.IsConnectedAndInRoom || playerService.IsMasterClient || !isSynced)
            {
                isRunning = true;
                if (playerService.IsConnectedAndInRoom && playerService.IsMasterClient && isSynced)
                {
                    NetworkServiceLocator.DomainRpcService?.InvokeRPC("RPC_SyncStartTime", NetworkTarget.Others);
                }
                EventChannelLocator.MainContainer.timeStartedChannel.Raise();
            }
        }

        public void PauseGameTime()
        {
            var playerService = NetworkServiceLocator.PlayerService;

            if (!playerService.IsConnectedAndInRoom || playerService.IsMasterClient || !isSynced)
            {
                isRunning = false;
                if (playerService.IsConnectedAndInRoom && playerService.IsMasterClient && isSynced)
                {
                    NetworkServiceLocator.DomainRpcService?.InvokeRPC("RPC_SyncPauseTime", NetworkTarget.Others);
                }
                EventChannelLocator.MainContainer.timePausedChannel.Raise();
            }
        }

        public void ResumeGameTime() => StartGameTime();

        public void ResetGameTime()
        {
            var playerService = NetworkServiceLocator.PlayerService;

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
                    NetworkServiceLocator.DomainRpcService?.InvokeRPC("RPC_SyncResetTime", NetworkTarget.All);
                }
                EventChannelLocator.MainContainer.timeResetChannel.Raise();
            }
        }

        public void SetTime(float time)
        {
            var playerService = NetworkServiceLocator.PlayerService;

            time = Mathf.Clamp(time, 0f, totalGameTime);
            if (!playerService.IsConnectedAndInRoom || playerService.IsMasterClient || !isSynced)
            {
                currentTime = time;
                if (playerService.IsConnectedAndInRoom && playerService.IsMasterClient && isSynced)
                {
                    NetworkServiceLocator.DomainRpcService?.InvokeRPC("RPC_SyncSetTime", NetworkTarget.Others, time);
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