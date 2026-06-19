using System.Collections;
using System.Collections.Generic;
using Manager;
using Photon.Pun;
using UnityEngine;

namespace Enemies
{
    public class EnemiesGenorator : MonoBehaviourPun
    {
        [SerializeField]
        GameObject testPrefab;
        private bool isPhotonReady = false; // Photon是否准备就绪
        private float spawnInterval; // 生成间隔
        private float updateSpawnInterval = 30f; // 更新生成间隔的时间
        private float updateSpawnIntervalCounter = 0f; // 
        private float timer = 0f; // 计时器
        bool canGenorate = false;

        void Start()
        {
            float dc = DifficultyCoefficientManager.instance.GetDifficultyCoefficient();
            spawnInterval = 10/dc;
        }

        void Update()
        {
            if (GamePauseManager.isPaused)
                return;
            if (!PhotonNetwork.IsMasterClient)
                return; // 只有房主执行生成逻辑
            if (!isPhotonReady)
            {
                // 更清晰的方式
                if (GameManager.isTest)
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
                    timer += Time.deltaTime;
                }
            }
        }

        private void UpdateSpawnInterval()
        {
            updateSpawnIntervalCounter += Time.deltaTime;
            if(updateSpawnIntervalCounter >= updateSpawnInterval)
            {
                float dc = 1;
                if (SyncedGameTimeManager.Instance.GetTotalGameTime() > 600f && SyncedGameTimeManager.Instance.GetTotalGameTime() < 1200f)
                {
                    dc = DifficultyCoefficientManager.instance.GetDifficultyCoefficient() * 1;
                }else if (SyncedGameTimeManager.Instance.GetTotalGameTime() > 900f)
                {
                    dc = DifficultyCoefficientManager.instance.GetDifficultyCoefficient() * 2f;
                }else if (SyncedGameTimeManager.Instance.GetTotalGameTime() > 1200f)
                {
                    dc = DifficultyCoefficientManager.instance.GetDifficultyCoefficient() * 3f;
                }
                else
                {
                    dc = DifficultyCoefficientManager.instance.GetDifficultyCoefficient();
                }
                    spawnInterval = 8/dc;
                if (SyncedGameTimeManager.Instance.GetIsGenerated())
                {
                    spawnInterval = 6f;
                }
            updateSpawnIntervalCounter = 0f;
            }
        }

        private void SpawnEnemy()
        {
            if (GamePauseManager.isPaused)
                return;
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }
            // 生成敌人
            NetworkObjectPoolManager.instance.Spawn(
                NetworkObjectPoolConst.Skeleton,
                transform.position
            );
        }
    }
}
