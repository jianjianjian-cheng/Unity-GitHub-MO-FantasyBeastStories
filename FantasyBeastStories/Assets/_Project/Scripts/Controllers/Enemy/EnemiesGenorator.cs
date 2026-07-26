using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Controllers.Enemy;
using Core.Channels.Game;
using Core;
using Core.Contracts;
using Core.Network;
using Controllers.Time;
using Controllers.Network;
using Controllers.Player;
using Managers;

namespace Controllers.Enemy
{
    public class EnemiesGenorator : MonoBehaviour
    {
        [Header("生成配置")]
        [SerializeField, Tooltip("波次配置 SO（预制体、池参数、生成间隔等）")]
        private WaveConfigSO waveConfig;

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
            if (waveConfig == null)
            {
                Debug.LogError($"[EnemiesGenorator] {gameObject.name} 的 WaveConfigSO 未配置！", this);
                return;
            }

            if (string.IsNullOrEmpty(waveConfig.poolName))
                actualPoolName = gameObject.name + "_Pool";
            else
                actualPoolName = waveConfig.poolName;

            dragonPoolName = PoolConst.Dragon;

            if (NetworkObjectPoolManager.instance != null && waveConfig.enemyPrefab != null)
            {
                NetworkObjectPoolManager.instance.RegisterPool(actualPoolName, waveConfig.enemyPrefab, waveConfig.poolPreloadCount);
                isPoolRegistered = true;
            }

            // 注册 Dragon 池（如果配置了预制体且池尚未注册）
            if (NetworkObjectPoolManager.instance != null && waveConfig.dragonPrefab != null)
            {
                NetworkObjectPoolManager.instance.RegisterPool(dragonPoolName, waveConfig.dragonPrefab, waveConfig.dragonPoolPreloadCount);
                isDragonPoolRegistered = true;
            }

            spawnInterval = waveConfig.baseSpawnInterval;
        }

        private float _diagTimer = 0f;

        void Update()
        {
            if (waveConfig == null)
                return;
            if (GamePauseManager.isPaused)
            {
                // 每秒打印一次暂停诊断
                _diagTimer += UnityEngine.Time.deltaTime;
                if (_diagTimer >= 1f)
                {
                    _diagTimer = 0f;
                    var tm = ServiceLocator.Get<SyncedGameTimeManager>();
                    Debug.LogWarning($"[EnemiesGenorator] ⏸ 暂停中，无法生成 | gameTime={tm?.GetCurrentTime():F0}s | frame={UnityEngine.Time.frameCount}");
                }
                return;
            }
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

                int playerCount = PlayerManager.instance != null ? PlayerManager.instance.PlayerCount : 1;
                float countMultiplier = EnemyScalingCalculator.GetCountMultiplier(currentTime)
                                        * EnemyScalingCalculator.GetPlayerCountMultiplier(playerCount);

                // 基础间隔 × 间隔倍率（1/数量倍率）
                spawnInterval = waveConfig.baseSpawnInterval / countMultiplier;

                // 确保不低于最小间隔
                spawnInterval = Mathf.Max(spawnInterval, waveConfig.minSpawnInterval);

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
            int playerCount = PlayerManager.instance != null ? PlayerManager.instance.PlayerCount : 1;
            float countMultiplier = EnemyScalingCalculator.GetCountMultiplier(currentTime)
                                    * EnemyScalingCalculator.GetPlayerCountMultiplier(playerCount);
            float maxCountMultiplier = EnemyScalingCalculator.GetPlayerMaxCountMultiplier(playerCount);

            // 决定生成 Skeleton 还是 Dragon
            bool spawnDragon = false;
            if (isDragonPoolRegistered || waveConfig.dragonPrefab != null)
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
                // 动态上限 = 配置上限 × 玩家数量倍率 × 时间数量倍率
                int dynamicMaxCount = Mathf.RoundToInt(baseMaxCount * maxCountMultiplier * countMultiplier);
                int currentCount = _countMonitor.GetCount(targetPoolName);
                // 调试日志：每 10 次生成输出一次状态
                if (UnityEngine.Time.frameCount % 600 == 0)
                {
                    Debug.Log($"[EnemiesGenorator] {targetPoolName} → current={currentCount}/{dynamicMaxCount} | time={currentTime:F0}s | players={playerCount} | countMul={countMultiplier:F2} | maxMul={maxCountMultiplier:F2} | interval={spawnInterval:F2}s");
                }
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
                            dynamicMaxCount = Mathf.RoundToInt(baseMaxCount * maxCountMultiplier * countMultiplier);
                            currentCount = _countMonitor.GetCount(targetPoolName);
                            if (currentCount >= dynamicMaxCount)
                            {
                                Debug.LogWarning($"[EnemiesGenorator] 🚫 两个池均已满 → {dragonPoolName}={_countMonitor.GetCount(dragonPoolName)}/{dynamicMaxCount}, {actualPoolName}={currentCount}/{dynamicMaxCount} | time={currentTime:F0}s");
                                return;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[EnemiesGenorator] 🚫 Skeleton池已满 → {actualPoolName}={currentCount}/{dynamicMaxCount} | time={currentTime:F0}s");
                        return;
                    }
                }
            }

            // 确保 Dragon 池已注册（延迟注册）
            if (spawnDragon && !isDragonPoolRegistered)
            {
                if (NetworkObjectPoolManager.instance != null && waveConfig.dragonPrefab != null)
                {
                    NetworkObjectPoolManager.instance.RegisterPool(dragonPoolName, waveConfig.dragonPrefab, waveConfig.dragonPoolPreloadCount);
                    isDragonPoolRegistered = true;
                }
                else
                    return;
            }

            // 确保 Skeleton 池已注册（延迟注册）
            if (!spawnDragon && !isPoolRegistered)
            {
                if (NetworkObjectPoolManager.instance != null && waveConfig.enemyPrefab != null)
                {
                    NetworkObjectPoolManager.instance.RegisterPool(actualPoolName, waveConfig.enemyPrefab, waveConfig.poolPreloadCount);
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
