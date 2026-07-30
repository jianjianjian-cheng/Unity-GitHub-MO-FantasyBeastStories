using System.Collections.Generic;
using Core;
using Core.Channels.Game;
using Core.Channels.General;
using Core.Channels.Task;
using Controllers.Player;
using Core.Contracts;
using Core.Network;
using Controllers.Task;
using Controllers.Time;
using UnityEngine;
using Core.SharedModel;
using Controllers.Task;
using Controllers.Time;

namespace Controllers.Time
{
    /// <summary>
    /// 时间事件协调器（Application 层）
    ///
    /// 职责：
    /// - 监听时间线事件，根据事件 ID 激活对应任务（KillSacrifice / EscortRobot / TestBoss）
    /// - 监听游戏状态变化（GameOver）
    /// - 通过 EventChannel 与 TaskManager 通信
    ///
    /// 通信方式：
    /// 输入 → timeEventChannel（时间线事件）
    /// 输入 → gameStateChangeChannel（游戏状态变化）
    /// 输出 → taskActivationChannel（激活任务）
    /// 输出 → taskNoticeChannel（发送任务通知）
    /// </summary>
    public class TimeEventCoordinator : MonoBehaviour
    {
        void OnEnable()
        {
            EventChannelLocator.MainContainer.timeEventChannel.RegisterListener(OnTimeEventReceived);
            EventChannelLocator.MainContainer.gameStateChangeChannel.RegisterListener(OnGameStateChanged);
        }

        void OnDisable()
        {
            EventChannelLocator.MainContainer.timeEventChannel.UnregisterListener(OnTimeEventReceived);
            EventChannelLocator.MainContainer.gameStateChangeChannel.UnregisterListener(OnGameStateChanged);
        }

        #region 时间事件处理

        void OnTimeEventReceived(EventArgsBase args)
        {
            var timeArgs = args as TimeEventArgs;
            if (timeArgs != null)
            {
                if (timeArgs.eventData == null) return;
                Debug.Log($"收到事件: {timeArgs.eventData.eventName}");
                OnTimeEventTriggered(timeArgs.eventData);
            }
        }

        void OnTimeEventTriggered(TimeEventData eventData)
        {
            Debug.Log($"事件触发: {eventData.eventName} at {eventData.triggerTime}秒");
            SetNotice(eventData.eventName, eventData.description, eventData.limittime, eventData.requireCount);
            switch (eventData.eventId)
            {
                case "KillSacrifice":
                    KillSacrifice(eventData);
                    break;
                case "EscortRobot":
                    EscortRobot(eventData);
                    break;
                case "TestBoss":
                    TestBoss(eventData);
                    break;
            }
        }

        private void KillSacrifice(TimeEventData eventData)
        {
            Debug.Log($"任务激活: {eventData.eventName}" + eventData.description);
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                return;
            }

            Vector3 randomPosition = GetRandomPlayerPosition();

            KillTask killTask = new KillTask(
                TaskConst.KillSacrifice,
                randomPosition,
                7f,
                eventData.requireCount,
                eventData.limittime
            );

            EventChannelLocator.MainContainer.taskActivationChannel.Raise(killTask);
        }

        private void EscortRobot(TimeEventData eventData)
        {
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                return;
            }
            Debug.Log($"任务激活: {eventData.eventName}" + eventData.description);

            Vector3 randomPosition = GetRandomPlayerPosition();

            EscortTask escortTask = new EscortTask(
                eventData.eventId,
                randomPosition,
                3,
                eventData.requireCount,
                eventData.limittime
            );

            EventChannelLocator.MainContainer.taskActivationChannel.Raise(escortTask);
        }

        private void TestBoss(TimeEventData eventData)
        {
            // 预留：Boss 事件
        }

        private void SetNotice(string name, string description, int limitTime, int requireCount)
        {
            EventChannelLocator.MainContainer.taskNoticeChannel.Raise(
                new TaskNoticeData(name, description, limitTime, requireCount)
            );
        }

        private Vector3 GetRandomPlayerPosition()
        {
            IReadOnlyList<GameObject> players =
                ServiceLocator.Get<PlayerManager>() != null ? ServiceLocator.Get<PlayerManager>().ActivePlayerObjects : null;
            if (players == null || players.Count == 0)
            {
                Debug.LogError("没有玩家在线，无法激活任务");
            }

            GameObject randomPlayer = players[Random.Range(0, players.Count)];
            Vector3 randomDirection = Random.insideUnitSphere;
            randomDirection.y = 0;
            randomDirection.Normalize();

            float randomDistance = Random.Range(20f, 30f);
            Vector3 randomPosition =
                randomPlayer.transform.position + randomDirection * randomDistance;

            randomPosition.y = 1f;
            return randomPosition;
        }

        void OnGameFinished()
        {
            Debug.Log("游戏时间结束！");
        }

        void OnGameStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
            {
                Debug.Log("游戏时间结束！");
            }
        }

        #endregion
    }
}