using System.Collections;
using System.Collections.Generic;
using Domain.Event;
using Domain.Event.Channels.Game;
using Domain.Event.Channels.Player;
using Domain.Time;
using UnityEngine;

namespace Application
{
    public class DifficultyCoefficientManager : MonoBehaviour
    {
        [Header("难度系数设置")]
        private float difficultyCoefficient = 1f;

        void Awake()
        {
            InitDifficultyCoefficient();
        }

        void OnEnable()
        {
            EventChannelLocator.MainContainer.timeChangeEnemyAttributeChannel.RegisterListener(UpdateDifficultyCoefficient);
            EventChannelLocator.MainContainer.difficultyCoefficientQueryChannel.RegisterListener(OnDifficultyCoefficientQuery);
        }

        void OnDisable()
        {
            EventChannelLocator.MainContainer.timeChangeEnemyAttributeChannel.UnregisterListener(UpdateDifficultyCoefficient);
            EventChannelLocator.MainContainer.difficultyCoefficientQueryChannel.UnregisterListener(OnDifficultyCoefficientQuery);
        }

        private void OnDifficultyCoefficientQuery(DifficultyCoefficientQueryData data)
        {
            switch (data.queryType)
            {
                case DifficultyQueryType.GetDifficultyCoefficient:
                    data.result = difficultyCoefficient;
                    break;
                case DifficultyQueryType.GetPlayerCount:
                    var playerQuery = new PlayerQueryData(PlayerQueryType.GetPlayerCount);
                    EventChannelLocator.MainContainer.playerQueryChannel.Raise(playerQuery);
                    data.playerCount = playerQuery.intResult;
                    break;
            }
        }

        private void InitDifficultyCoefficient()
        {
            int playerCount = 1;
            if (!EventChannelLocator.MainContainer.gameSettings.IsTest)
            {
                var queryData = new DifficultyCoefficientQueryData { queryType = DifficultyQueryType.GetPlayerCount };
                OnDifficultyCoefficientQuery(queryData);
                playerCount = queryData.playerCount;
            }
            switch (playerCount)
            {
                case 1:
                    difficultyCoefficient = 0.7f;
                    break;
                case 2:
                    difficultyCoefficient = 1f;
                    break;
                case 3:
                    difficultyCoefficient = 1.25f;
                    break;
                case 4:
                    difficultyCoefficient = 1.5f;
                    break;
                default:
                    difficultyCoefficient = 1f;
                    break;
            }
        }

        public float GetDifficultyCoefficient()
        {
            return difficultyCoefficient;
        }


        /// <summary>
        /// 根据游戏进行的时间更新难度系数
        /// </summary>
        /// <param name="currentTime"></param>
        public void UpdateDifficultyCoefficient(float currentTime)
        {
            float totalTime = SyncedGameTimeManager.Instance.GetTotalGameTime();
            float progress = Mathf.Clamp01(currentTime / totalTime);
            difficultyCoefficient = 1.0f + progress * 0.5f;
        }
    }
}
