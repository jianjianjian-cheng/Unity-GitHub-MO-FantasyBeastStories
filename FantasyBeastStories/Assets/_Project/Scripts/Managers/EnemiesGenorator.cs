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

        [Header("生成间隔设置")]
        [SerializeField, Tooltip("初始生成间隔（秒）")]
        private float baseSpawnInterval = 10f;

        [SerializeField, Tooltip("最小生成间隔（秒），数量峰值时的最快频率")]
        private float minSpawnInterval = 1.5f;

        private string actualPoolName;
        private bool isPoolRegistered = false;
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

            if (NetworkObjectPoolManager.instance != null && testPrefab != null)
            {
                NetworkObjectPoolManager.instance.RegisterPool(actualPoolName, testPrefab, poolPreloadCount);
                isPoolRegistered = true;
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
                // 数量倍率 1x → 间隔倍率 1.0（10s）
                // 数量倍率 2x → 间隔倍率 0.5（5s）
                // 数量倍率 0.5x → 间隔倍率 2.0（20s，Boss出现后减速）
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

            // 获取当前数量倍率，动态调整 maxCount 上限
            var timeManager = ServiceLocator.Get<SyncedGameTimeManager>();
            float currentTime = timeManager != null ? timeManager.GetCurrentTime() : 0f;
            float countMultiplier = EnemyScalingCalculator.GetCountMultiplier(currentTime);

            int baseMaxCount = _countMonitor != null
                ? _countMonitor.GetMaxCount(actualPoolName)
                : -1;

            if (baseMaxCount > 0)
            {
                // 动态上限 = 配置上限 × 数量倍率
                int dynamicMaxCount = Mathf.RoundToInt(baseMaxCount * countMultiplier);
                int currentCount = _countMonitor.GetCount(actualPoolName);
                if (currentCount >= dynamicMaxCount)
                {
                    return;
                }
            }

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

            EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                PoolOperationData.CreateSpawn(actualPoolName, transform.position, Quaternion.identity, null));
        }
    }
}
