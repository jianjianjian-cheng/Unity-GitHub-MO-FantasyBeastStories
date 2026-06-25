using System.Collections;
using System.Collections.Generic;
using Infrastructure.Network;
using Domain.Manager;
using Photon.Pun;
using UnityEngine;
using Domain.Event.Channels.Game;
using Domain.Event;
using Domain.Time;

namespace Domain.Enemy
{
    public class EnemiesGenorator : MonoBehaviourPun
    {
        [SerializeField]
        GameObject testPrefab;
        private bool isPhotonReady = false;
        private float spawnInterval;
        private float updateSpawnInterval = 30f;
        private float updateSpawnIntervalCounter = 0f;
        private float timer = 0f;
        bool canGenorate = false;

        private float QueryDifficultyCoefficient()
        {
            var query = new DifficultyCoefficientQueryData();
            EventChannelLocator.MainContainer.difficultyCoefficientQueryChannel.Raise(query);
            return query.result;
        }

        void Start()
        {
            float dc = QueryDifficultyCoefficient();
            spawnInterval = 10 / dc;
        }

        void Update()
        {
            if (EventChannelLocator.MainContainer.gameSettings.IsPaused)
                return;
            if (!PhotonNetwork.IsMasterClient)
                return; // 只有房主执行生成逻辑
            if (!isPhotonReady)
            {
                // 更清晰的方式
                if (EventChannelLocator.MainContainer.gameSettings.IsTest)
                {
                    canGenorate = true; // 测试模式：无条件允许
                }
                else
                {
                    canGenorate =
                        PhotonNetwork.IsConnectedAndReady
                        && PhotonNetwork.InRoom
                        && PhotonNetwork.IsMasterClient;
                    if (canGenorate)
                    {
                        isPhotonReady = true;
                    }
                }
            }
            if (canGenorate)
            {
                if (timer >= spawnInterval)
                {
                    SpawnEnemy();
                    timer = 0f;
                }
                else
                {
                    timer += UnityEngine.Time.deltaTime;
                }
            }
        }

        private void UpdateSpawnInterval()
        {
            updateSpawnIntervalCounter += UnityEngine.Time.deltaTime;
            if (updateSpawnIntervalCounter >= updateSpawnInterval)
            {
                float dc = 1;
                if (SyncedGameTimeManager.Instance.GetTotalGameTime() > 600f && SyncedGameTimeManager.Instance.GetTotalGameTime() < 1200f)
                {
                    dc = QueryDifficultyCoefficient() * 1;
                }
                else if (SyncedGameTimeManager.Instance.GetTotalGameTime() > 900f)
                {
                    dc = QueryDifficultyCoefficient() * 2f;
                }
                else if (SyncedGameTimeManager.Instance.GetTotalGameTime() > 1200f)
                {
                    dc = QueryDifficultyCoefficient() * 3f;
                }
                else
                {
                    dc = QueryDifficultyCoefficient();
                }
                spawnInterval = 8 / dc;
                if (SyncedGameTimeManager.Instance.GetIsGenerated())
                {
                    spawnInterval = 6f;
                }
                updateSpawnIntervalCounter = 0f;
            }
        }

        private void SpawnEnemy()
        {
            if (EventChannelLocator.MainContainer.gameSettings.IsPaused)
                return;
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }
            // 生成敌人
            EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                PoolOperationData.CreateSpawn(NetworkObjectPoolConst.Skeleton, transform.position, Quaternion.identity, null));
        }
    }
}