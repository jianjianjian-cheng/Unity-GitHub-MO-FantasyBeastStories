using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Controllers.Enemy;
using Core.Channels.Game;
using Core;
using Controllers.Services;
using Controllers.Time;
using Controllers.Network;

namespace Managers
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

        [Header("Dragon 生成设置")]
        [SerializeField, Tooltip("Dragon 预制体（留空则不生成 Dragon）")]
        private GameObject dragonPrefab;

        [SerializeField, Tooltip("Dragon 对象池预创建数量")]
        private int dragonPoolPreloadCount = 10;

        [Header("生成间隔设置")]
        [SerializeField, Tooltip("初始生成间隔（秒）")]
        private float baseSpawnInterval = 10f;

        [SerializeField, Tooltip("最小生成间隔（秒），数量峰值时的最快频率")]
        private float minSpawnInterval = 1.5f;

        private string actualPoolName;
        private string dragonPoolName;
        private bool isPoolRegistered = false;
        private bool isDragonPoolRegistered = false;
        private bool isPhotonReady = false;
        private float spawnInterval;
        private float updateSpawnInterval = 30f;
        private float updateSpawnIntervalCounter = 0f;
        private float timer = 0f;
        bool canGenorate = false;

        /// <summary>缓存的怪物数量监控器</summary>
        private MonsterCountMonitor _countMonitor;

        void Start()
        {
            if (string.IsNullOrEmpty(poolName))
                actualPoolName = gameObject.name + "_Pool";
            else
                actualPoolName = poolName;

            dragonPoolName = PoolConst.Dragon;

            if (NetworkObjectPoolManager.instance != null && testPrefab != null)
            {
                NetworkObjectPoolManager.instance.RegisterPool(actualPoolName, testPrefab, poolPreloadCount);
                isPoolRegistered = true;
            }

            // 注册 Dragon 池（如果配置了预制体且池尚未注册）
            if (NetworkObjectPoolManager.instance != null && dragonPrefab != null)
            {
                NetworkObjectPoolManager.instance.RegisterPool(dragonPoolName, dragonPrefab, dragonPoolPreloadCount);
                isDragonPoolRegistered = true;
            }

            spawnInterval = baseSpawnInterval;
        }

        void Update()
        {
            if (GamePauseManager.isPaused)
                return;
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
                return;
            if (!isPhotonReady)
            {
                if (EventChannelLocator.MainContainer.gameSettings.IsTest)
                {
                    canGenorate = true;
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
        /// 根据 EnemyScalingCalculator 的数量倍率动态更新生成间隔
        /// 数量倍率越高 → 生成越快（间隔越短）
        /// </summary>
        private void UpdateSpawnInterval()
        {
            updateSpawnIntervalCounter += UnityEngine.Time.deltaTime;
            if (updateSpawnIntervalCounter >= updateSpawnInterval)
            {
                var timeManager = ServiceLocator.Get<SyncedGameTimeManager>();
                float currentTime = timeManager != null ? timeManager.GetCurrentTime() : 0f;

                // 基础间隔 × 间隔倍率（1/数量倍率）
                float intervalMultiplier = EnemyScalingCalculator.GetSpawnIntervalMultiplier(currentTime);
                spawnInterval = baseSpawnInterval * intervalMultiplier;

                // 确保不低于最小间隔
                spawnInterval = Mathf.Max(spawnInterval, minSpawnInterval);

                updateSpawnIntervalCounter = 0f;
            }
        }

        private void SpawnEnemy()
        {
            if (GamePauseManager.isPaused)
                return;
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
                return;

            if (_countMonitor == null)
                _countMonitor = ServiceLocator.Get<MonsterCountMonitor>();

            // 获取当前游戏时间
            var timeManager = ServiceLocator.Get<SyncedGameTimeManager>();
            float currentTime = timeManager != null ? timeManager.GetCurrentTime() : 0f;
            float countMultiplier = EnemyScalingCalculator.GetCountMultiplier(currentTime);

            // 决定生成 Skeleton 还是 Dragon
            bool spawnDragon = false;
            if (isDragonPoolRegistered || dragonPrefab != null)
            {
                float dragonProbability = EnemyScalingCalculator.GetDragonSpawnProbability(currentTime);
                spawnDragon = Random.value <= dragonProbability;
            }

            string targetPoolName = spawnDragon ? dragonPoolName : actualPoolName;

            // 检查目标池的数量上限
            int baseMaxCount = _countMonitor != null
                ? _countMonitor.GetMaxCount(targetPoolName)
                : -1;

            if (baseMaxCount > 0)
            {
                // 动态上限 = 配置上限 × 数量倍率
                int dynamicMaxCount = Mathf.RoundToInt(baseMaxCount * countMultiplier);
                int currentCount = _countMonitor.GetCount(targetPoolName);
                if (currentCount >= dynamicMaxCount)
                {
                    // Dragon 池满了，尝试生成 Skeleton
                    if (spawnDragon)
                    {
                        targetPoolName = actualPoolName;
                        baseMaxCount = _countMonitor != null
                            ? _countMonitor.GetMaxCount(targetPoolName)
                            : -1;
                        if (baseMaxCount > 0)
                        {
                            dynamicMaxCount = Mathf.RoundToInt(baseMaxCount * countMultiplier);
                            currentCount = _countMonitor.GetCount(targetPoolName);
                            if (currentCount >= dynamicMaxCount)
                                return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }

            // 确保 Dragon 池已注册（延迟注册）
            if (spawnDragon && !isDragonPoolRegistered)
            {
                if (NetworkObjectPoolManager.instance != null && dragonPrefab != null)
                {
                    NetworkObjectPoolManager.instance.RegisterPool(dragonPoolName, dragonPrefab, dragonPoolPreloadCount);
                    isDragonPoolRegistered = true;
                }
                else
                    return;
            }

            // 确保 Skeleton 池已注册（延迟注册）
            if (!spawnDragon && !isPoolRegistered)
            {
                if (NetworkObjectPoolManager.instance != null && testPrefab != null)
                {
                    NetworkObjectPoolManager.instance.RegisterPool(actualPoolName, testPrefab, poolPreloadCount);
                    isPoolRegistered = true;
                }
                else
                    return;
            }

            EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                PoolOperationData.CreateSpawn(targetPoolName, transform.position, Quaternion.identity, null));
        }
    }
}
