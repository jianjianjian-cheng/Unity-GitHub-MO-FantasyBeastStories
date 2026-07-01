using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Domain.Event.Channels.Game;
using Domain.Event;
using Domain.Services;
using Domain.Time;
using Infrastructure.Network;

namespace Application
{
    public class EnemiesGenorator : MonoBehaviour
    {
        [SerializeField]
        GameObject testPrefab;

        [Header("对象池设置")]
        [SerializeField, Tooltip("专属对象池名称，多个生成器使用不同池名可避免竞争")]
        private string poolName = "";

        [SerializeField, Tooltip("该生成器专属对象池的预创建数量（骷髅敌人数量多建议 20-30）")]
        private int poolPreloadCount = 25;

        private string actualPoolName;
        private bool isPoolRegistered = false;
        private bool isPhotonReady = false;
        private float spawnInterval;
        private float updateSpawnInterval = 30f;
        private float updateSpawnIntervalCounter = 0f;
        private float timer = 0f;
        bool canGenorate = false;

        /// <summary>
        /// 最低生成间隔（秒），在 10 分钟时达到此值
        /// </summary>
        private const float MinSpawnInterval = 1.5f;

        /// <summary>
        /// 初始生成间隔（秒）
        /// </summary>
        private const float BaseSpawnInterval = 10f;

        /// <summary>
        /// 达到最低生成间隔所需时间（秒）= 10 分钟
        /// </summary>
        private const float TimeToReachMin = 600f;

        /// <summary>
        /// 回到初始生成间隔所需时间（秒）= 15 分钟
        /// </summary>
        private const float TimeToReturnToBase = 900f;

        private float QueryDifficultyCoefficient()
        {
            var query = new DifficultyCoefficientQueryData { queryType = DifficultyQueryType.GetDifficultyCoefficient };
            EventChannelLocator.MainContainer.difficultyCoefficientQueryChannel.Raise(query);
            return query.result;
        }

        void Start()
        {
            // 池名未设置时自动按 GameObject 名称生成，确保每个生成器拥有独立池
            if (string.IsNullOrEmpty(poolName))
                actualPoolName = gameObject.name + "_Pool";
            else
                actualPoolName = poolName;

            // 在对象池管理器中注册专属池（已存在则跳过）
            if (NetworkObjectPoolManager.instance != null && testPrefab != null)
            {
                NetworkObjectPoolManager.instance.RegisterPool(actualPoolName, testPrefab, poolPreloadCount);
                isPoolRegistered = true;
            }

            spawnInterval = BaseSpawnInterval;
        }

        void Update()
        {
            if (GamePauseManager.isPaused)
                return;
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
                return; // 只有房主执行生成逻辑
            if (!isPhotonReady)
            {
                if (EventChannelLocator.MainContainer.gameSettings.IsTest)
                {
                    canGenorate = true; // 测试模式：无条件允许
                }
                else
                {
                    canGenorate =
                        NetworkServiceLocator.PlayerService.IsConnectedAndInRoom;
                    if (canGenorate)
                    {
                        isPhotonReady = true;
                    }
                }
            }
            if (canGenorate)
            {
                // 定期根据游戏进度动态更新生成间隔
                UpdateSpawnInterval();

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

        /// <summary>
        /// 根据游戏已过时间动态更新生成间隔。
        /// 曲线：0→10min 从 10s 降到 1.5s，10→15min 从 1.5s 回到 10s。
        /// 不受难度影响。
        /// </summary>
        private void UpdateSpawnInterval()
        {
            updateSpawnIntervalCounter += UnityEngine.Time.deltaTime;
            if (updateSpawnIntervalCounter >= updateSpawnInterval)
            {
                var timeManager = ServiceLocator.Get<SyncedGameTimeManager>();
                float currentTime = timeManager.GetCurrentTime();

                if (currentTime <= TimeToReachMin)
                {
                    // 0 → 10 分钟：从 10s 线性下降到 1.5s
                    float progress = currentTime / TimeToReachMin;
                    spawnInterval = Mathf.Lerp(BaseSpawnInterval, MinSpawnInterval, progress);
                }
                else if (currentTime <= TimeToReturnToBase)
                {
                    // 10 → 15 分钟：从 1.5s 线性回到 10s
                    float progress = (currentTime - TimeToReachMin) / (TimeToReturnToBase - TimeToReachMin);
                    spawnInterval = Mathf.Lerp(MinSpawnInterval, BaseSpawnInterval, progress);
                }
                else
                {
                    // 15 分钟后：保持初始值 10s
                    spawnInterval = BaseSpawnInterval;
                }

                updateSpawnIntervalCounter = 0f;
            }
        }

        private void SpawnEnemy()
        {
            if (GamePauseManager.isPaused)
                return;
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
                return;
            if (!isPoolRegistered)
            {
                Debug.LogWarning($"[EnemiesGenorator] {gameObject.name} 对象池未注册，尝试延迟注册");
                if (NetworkObjectPoolManager.instance != null && testPrefab != null)
                {
                    NetworkObjectPoolManager.instance.RegisterPool(actualPoolName, testPrefab, poolPreloadCount);
                    isPoolRegistered = true;
                }
                else
                    return;
            }

            // 使用专属对象池生成敌人，避免多个生成器竞争同一池
            EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                PoolOperationData.CreateSpawn(actualPoolName, transform.position, Quaternion.identity, null));
        }
    }
}