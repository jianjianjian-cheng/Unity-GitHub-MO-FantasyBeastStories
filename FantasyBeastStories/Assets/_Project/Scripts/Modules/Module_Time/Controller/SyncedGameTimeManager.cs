using System;
using System.Collections.Generic;
using Core;
using Core.SharedModel;
using Core.Contracts;
using Core.Network;
using Core.Channels.Game;
using Controllers.Time;
using Managers;
using NetworkTarget = Controllers.Network.NetworkTarget;
using UnityEngine;

namespace Controllers.Time
{
    /// <summary>
    /// 游戏时间控制器 — 薄层 MonoBehaviour，持有 GameTimeModel 实例。
    ///
    /// 职责：
    /// - 生命周期管理（单例 + ServiceLocator 注册）
    /// - Update 循环驱动 Model 时间推进
    /// - 网络同步（RPC 发送 / 接收）
    /// - EventChannelSO 通知 View 层
    /// - 外部依赖处理（NetworkServiceLocator / Time.deltaTime）
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

        [Header("最终Boss设置")]
        [SerializeField]
        private string bossName = "Boss_Horror";

        

        /// <summary>时间模型实例（纯 C#，可单测）</summary>
        public GameTimeModel Model { get; private set; }

        void Awake()
        {
            
            ServiceLocator.Register(this);

            Model = new GameTimeModel(totalGameTime, loop, bossName);

            if (timeEventList != null)
            {
                Model.LoadEvents(timeEventList.GetEvents());
                Debug.Log($"[SyncedGameTimeManager] 从 {timeEventList.name} 加载了 {Model.TimeEvents.Count} 个时间事件");
            }
            else
            {
                Debug.LogWarning("[SyncedGameTimeManager] timeEventList 未赋值，使用空的运行时列表");
            }

            // Model 事件 → EventChannelSO 转发
            Model.OnTimeUpdated += time =>
            {
                var args = new TimeEventArgs(null, time);
                EventChannelLocator.MainContainer.timeEventChannel.Raise(args);
            };

            Model.OnTimeEventTriggered += timeEvent =>
            {
                var args = new TimeEventArgs
                {
                    eventData = timeEvent,
                    currentTime = Model.CurrentTime,
                    isFromNetwork = false,
                };
                EventChannelLocator.MainContainer.timeEventChannel.Raise(args);
            };

            Model.OnGameTimeFinished += () =>
            {
                EventChannelLocator.MainContainer.gameStateChangeChannel.Raise(GameState.GameOver);
            };

            Model.OnBossSpawn += name =>
            {
                EventChannelLocator.MainContainer.bossSpawnChannel.Raise(name);
            };

            Model.OnEnemyAttributeChange += time =>
            {
                EventChannelLocator.MainContainer.timeChangeEnemyAttributeChannel.Raise(time);
            };
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
                EventChannelLocator.MainContainer.timeQueryChannel.RegisterListener(OnTimeQuery);

            if (EventChannelLocator.MainContainer.pauseChannel != null)
                EventChannelLocator.MainContainer.pauseChannel.RegisterListener(OnGamePauseChanged);
        }

        void OnDisable()
        {
            if (EventChannelLocator.MainContainer.timeQueryChannel != null)
                EventChannelLocator.MainContainer.timeQueryChannel.UnregisterListener(OnTimeQuery);

            if (EventChannelLocator.MainContainer.pauseChannel != null)
                EventChannelLocator.MainContainer.pauseChannel.UnregisterListener(OnGamePauseChanged);
        }

        void OnDestroy()
        {
            ServiceLocator.Unregister<SyncedGameTimeManager>();
        }

        private void OnTimeQuery(TimeQueryData data)
        {
            data.currentTime = Model.CurrentTime;
            data.normalizedTime = Model.GetNormalizedTime();
            data.totalGameTime = Model.TotalGameTime;
            data.remainingTime = Model.GetRemainingTime();
            data.isTimeRunning = Model.IsRunning;
        }

        private void OnGamePauseChanged(bool isPaused)
        {
            if (isPaused) Model.Pause();
            else Model.Resume();
        }

        // ──────────────────────────────────
        //  Update 循环
        // ──────────────────────────────────

        void Update()
        {
            if (!Model.IsRunning)
                return;

            var playerService = NetworkServiceLocator.PlayerService;
            bool isLocalAuthority = !isSynced
                                 || !playerService.IsConnectedAndInRoom
                                 || playerService.IsMasterClient;

            var op = Model.AdvanceTime(UnityEngine.Time.deltaTime);

            if (isLocalAuthority)
            {
                HandleSyncOp(op, playerService);
            }
        }

        private void HandleSyncOp(TimeSyncOp op, INetworkPlayerService playerService)
        {
            switch (op)
            {
                case TimeSyncOp.Loop:
                    if (playerService.IsConnectedAndInRoom && playerService.IsMasterClient && isSynced)
                        NetworkServiceLocator.DomainRpcService?.InvokeRPC("RPC_SyncResetTime", NetworkTarget.All);
                    break;

                case TimeSyncOp.Finished:
                    if (playerService.IsConnectedAndInRoom && playerService.IsMasterClient && isSynced)
                        NetworkServiceLocator.DomainRpcService?.InvokeRPC("RPC_GameTimeFinished", NetworkTarget.All);
                    break;

                case TimeSyncOp.BossSpawn:
                    if (playerService.IsMasterClient && isSynced)
                        NetworkServiceLocator.DomainRpcService?.InvokeRPC("RPC_BossSpawn", NetworkTarget.Others, bossName);
                    break;
            }
        }

        // ──────────────────────────────────
        //  静态 RPC Handler（供 ControllerRpcBridge 调用）
        // ──────────────────────────────────

        public static void HandleSyncStartCaTime()
        {
            if (!ServiceLocator.TryGet<SyncedGameTimeManager>(out var inst)) return;
            inst.StartGameTime();
        }

        public static void HandleOnTimeEventTriggered(string eventId, float triggerTime)
        {
            if (!ServiceLocator.TryGet<SyncedGameTimeManager>(out var inst)) return;
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
                inst.Model.MarkEventTriggered(eventId);
        }

        public static void HandleGameTimeFinished()
        {
            if (!ServiceLocator.TryGet<SyncedGameTimeManager>(out var inst)) return;
            inst.Model.FinishFromNetwork();
        }

        public static void HandleBossSpawn(string bossName)
        {
            if (!ServiceLocator.TryGet<SyncedGameTimeManager>(out var inst)) return;
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
                inst.Model.BossSpawnFromNetwork(bossName);
        }

        public static void HandleSyncSetTime(float time)
        {
            if (!ServiceLocator.TryGet<SyncedGameTimeManager>(out var inst)) return;
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
                inst.Model.SetTimeFromNetwork(time);
        }

        public static void HandleSyncStartTime()
        {
            if (!ServiceLocator.TryGet<SyncedGameTimeManager>(out var inst)) return;
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
                inst.Model.StartFromNetwork();
        }

        public static void HandleSyncPauseTime()
        {
            if (!ServiceLocator.TryGet<SyncedGameTimeManager>(out var inst)) return;
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
                inst.Model.PauseFromNetwork();
        }

        public static void HandleSyncResetTime()
        {
            if (!ServiceLocator.TryGet<SyncedGameTimeManager>(out var inst)) return;
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                inst.Model.Reset();
                inst.Model.StartFromNetwork();
            }
        }

        // ──────────────────────────────────
        //  公共控制方法
        // ──────────────────────────────────

        public void StartGameTime()
        {
            var playerService = NetworkServiceLocator.PlayerService;

            if (!playerService.IsConnectedAndInRoom || playerService.IsMasterClient || !isSynced)
            {
                Model.Start();
                if (playerService.IsConnectedAndInRoom && playerService.IsMasterClient && isSynced)
                    NetworkServiceLocator.DomainRpcService?.InvokeRPC("RPC_SyncStartTime", NetworkTarget.Others);
                EventChannelLocator.MainContainer.timeStartedChannel.Raise();
            }
        }

        public void PauseGameTime()
        {
            var playerService = NetworkServiceLocator.PlayerService;

            if (!playerService.IsConnectedAndInRoom || playerService.IsMasterClient || !isSynced)
            {
                Model.Pause();
                if (playerService.IsConnectedAndInRoom && playerService.IsMasterClient && isSynced)
                    NetworkServiceLocator.DomainRpcService?.InvokeRPC("RPC_SyncPauseTime", NetworkTarget.Others);
                EventChannelLocator.MainContainer.timePausedChannel.Raise();
            }
        }

        public void ResumeGameTime() => StartGameTime();

        public void ResetGameTime()
        {
            var playerService = NetworkServiceLocator.PlayerService;

            if (!playerService.IsConnectedAndInRoom || playerService.IsMasterClient || !isSynced)
            {
                Model.Reset();
                if (playerService.IsConnectedAndInRoom && playerService.IsMasterClient && isSynced)
                    NetworkServiceLocator.DomainRpcService?.InvokeRPC("RPC_SyncResetTime", NetworkTarget.All);
                EventChannelLocator.MainContainer.timeResetChannel.Raise();
            }
        }

        public void SetTime(float time)
        {
            var playerService = NetworkServiceLocator.PlayerService;

            if (!playerService.IsConnectedAndInRoom || playerService.IsMasterClient || !isSynced)
            {
                Model.SetTime(time);
                if (playerService.IsConnectedAndInRoom && playerService.IsMasterClient && isSynced)
                    NetworkServiceLocator.DomainRpcService?.InvokeRPC("RPC_SyncSetTime", NetworkTarget.Others, time);
            }
        }

        // ──────────────────────────────────
        //  事件列表管理
        // ──────────────────────────────────

        public void AddTimeEvent(TimeEventData timeEvent) => Model.AddTimeEvent(timeEvent);
        public void RemoveTimeEvent(string eventId) => Model.RemoveTimeEvent(eventId);

        // ──────────────────────────────────
        //  便捷转发（向后兼容）
        // ──────────────────────────────────

        public float GetCurrentTime() => Model.CurrentTime;
        public float GetNormalizedTime() => Model.GetNormalizedTime();
        public bool IsTimeRunning() => Model.IsRunning;
        public float GetRemainingTime() => Model.GetRemainingTime();
        public string FormatTime(float seconds) => Model.FormatTime(seconds);
        public float GetTotalGameTime() => Model.TotalGameTime;
        public bool GetIsGenerated() => Model.IsBossGenerated;
    }
}
