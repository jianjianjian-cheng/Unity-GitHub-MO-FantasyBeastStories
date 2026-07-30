using System.Collections.Generic;
using UnityEngine;
using Controllers.Battle;
using Core;
using Core.Contracts;
using Core.Network;
using Controllers.Experience;
using Controllers.Player;
using Controllers.Network;
using Photon.Pun;
using Core.SharedModel;

namespace Controllers.Battle
{
    /// <summary>
    /// 道具管理器 - 单例服务
    /// 职责：道具生成、生命周期管理、随机掉落
    /// 联机模式下采用非网络化方案（与经验球一致）：
    ///   房主生成 itemId → 广播 RPC → 各客户端本地生成 → 拾取时广播回收
    /// </summary>
    public class PowerUpManager : MonoBehaviour, IPowerUpService
    {
        

        [Header("道具配置")]
        [SerializeField] private List<PowerUpDataSO> availablePowerUps;
        [SerializeField] private GameObject powerUpPrefab;

        [Header("生成参数")]
        [SerializeField] private float spawnInterval = 30f; // 自动生成间隔
        [SerializeField] private int maxActivePowerUps = 5; // 场景最大数量
        [SerializeField] private float spawnRadius = 20f; // 生成范围
        [SerializeField] private bool autoSpawn = false; // 是否自动生成

        private List<GameObject> activePowerUps = new List<GameObject>();
        private float spawnTimer;
        private ObjectPoolManager poolManager;

        // ========== 道具非网络化（与经验球方案二一致） ==========
        /// <summary>下一个可用的 itemId 自增计数器（仅房主使用）</summary>
        private uint nextPowerUpId = 1;

        /// <summary>当前活跃的本地道具映射 itemId → GameObject（所有客户端使用）</summary>
        private Dictionary<uint, GameObject> activePowerUpsById = new Dictionary<uint, GameObject>();

        private void Awake()
        {
                  ServiceLocator.Register(this);
        }

        private void Start()
        {
            poolManager = ServiceLocator.Get<ObjectPoolManager>();
            ServiceLocator.Register<IPowerUpService>(this);
        }

        void OnDestroy()
        {
            ServiceLocator.Unregister<PowerUpManager>();
        }

        private void Update()
        {
            if (!autoSpawn) return;

            spawnTimer += UnityEngine.Time.deltaTime;
            if (spawnTimer >= spawnInterval && activePowerUps.Count < maxActivePowerUps)
            {
                SpawnRandomPowerUp(GetRandomPosition());
                spawnTimer = 0f;
            }
        }

        /// <summary>生成一个全局唯一的 itemId（仅房主调用）</summary>
        public uint GeneratePowerUpId()
        {
            return nextPowerUpId++;
        }

        public void SpawnPowerUp(PowerUpDataSO data, Vector3 position)
        {
            if (data == null || powerUpPrefab == null)
            {
                Debug.LogError("[PowerUpManager] 缺少必要配置！");
                return;
            }

            bool isTest = EventChannelLocator.MainContainer?.gameSettings?.IsTest ?? true;

            if (isTest)
            {
                // 测试模式：直接本地生成
                SpawnLocalPowerUp(0, position, data);
                return;
            }

            // 联机模式：仅房主生成 itemId 并广播 RPC 到所有客户端
            if (NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                uint itemId = GeneratePowerUpId();
                int itemIndex = availablePowerUps.IndexOf(data);
                NetworkServiceLocator.ObjectService.InvokeRPC(
                    ManagerRpcBridge.Instance, "RPC_SpawnPowerUp",
                    NetworkTarget.All, (int)itemId, position, itemIndex);
            }
        }

        public void SpawnRandomPowerUp(Vector3 position)
        {
            if (availablePowerUps.Count == 0) return;

            bool isTest = EventChannelLocator.MainContainer?.gameSettings?.IsTest ?? true;

            if (isTest)
            {
                // 测试模式：直接本地生成
                var randomData = GetWeightedRandom();
                SpawnLocalPowerUp(0, position, randomData);
                return;
            }

            // 联机模式：仅房主选择道具类型并广播
            if (NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                var randomData = GetWeightedRandom();
                uint itemId = GeneratePowerUpId();
                int itemIndex = availablePowerUps.IndexOf(randomData);
                NetworkServiceLocator.ObjectService.InvokeRPC(
                    ManagerRpcBridge.Instance, "RPC_SpawnPowerUp",
                    NetworkTarget.All, (int)itemId, position, itemIndex);
            }
        }

        public int GetActivePowerUpCount() => activePowerUps.Count;

        public void ClearAllPowerUps()
        {
            foreach (var obj in activePowerUps)
            {
                if (obj != null)
                    poolManager?.ReturnToPool(PoolConst.PowerUpItem, obj);
            }
            activePowerUps.Clear();
            activePowerUpsById.Clear();
            Debug.Log("[PowerUpManager] 清除所有道具");
        }

        public void RemoveFromActiveList(GameObject obj)
        {
            activePowerUps.Remove(obj);
        }

        private PowerUpDataSO GetWeightedRandom()
        {
            float totalWeight = 0f;
            foreach (var item in availablePowerUps)
                totalWeight += item.dropWeight;

            float randomPoint = Random.value * totalWeight;
            float currentWeight = 0f;

            foreach (var item in availablePowerUps)
            {
                currentWeight += item.dropWeight;
                if (randomPoint <= currentWeight)
                    return item;
            }

            return availablePowerUps[0];
        }

        private Vector3 GetRandomPosition()
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            return new Vector3(randomCircle.x, 0, randomCircle.y);
        }

        // ============================================================
        // 道具非网络化 RPC 处理（与经验球方案二一致）
        // ============================================================

        /// <summary>
        /// 由 ManagerRpcBridge.RPC_SpawnPowerUp 调用
        /// 每个客户端在本地生成一个道具（非网络对象）
        /// </summary>
        public static void HandleSpawnPowerUpRPC(uint itemId, Vector3 position, int itemIndex)
        {
            if (!ServiceLocator.TryGet<PowerUpManager>(out var inst)) return;
            inst.SpawnLocalPowerUp(itemId, position,
                itemIndex >= 0 && itemIndex < inst.availablePowerUps.Count
                    ? inst.availablePowerUps[itemIndex]
                    : null);
        }

        /// <summary>
        /// 由 ManagerRpcBridge.RPC_CollectPowerUp 调用
        /// 所有客户端隐藏对应的本地道具
        /// </summary>
        public static void HandleCollectPowerUpRPC(uint itemId)
        {
            if (!ServiceLocator.TryGet<PowerUpManager>(out var inst)) return;
            inst.HideLocalPowerUp(itemId);
        }

        /// <summary>
        /// RPC回调：所有客户端执行经验球飞向拾取者的动画
        /// </summary>
        public static void HandleMagnetCollectExpBallsRPC(int collectorActorNumber, float delay, float speed)
        {
            if (!ServiceLocator.TryGet<PowerUpManager>(out var inst)) return;

            GameObject collector = null;
            if (ServiceLocator.Get<PlayerManager>() != null)
            {
                foreach (var go in ServiceLocator.Get<PlayerManager>().ActivePlayerObjects)
                {
                    if (go == null) continue;
                    int ownerActor = NetworkServiceLocator.ObjectService.GetOwnerActorNumber(go.transform);
                    if (ownerActor == collectorActorNumber)
                    {
                        collector = go;
                        break;
                    }
                }
            }
            if (collector == null)
            {
                Debug.LogWarning("[PowerUpManager] 经验磁铁：未找到拾取者 GameObject");
                return;
            }

            bool isCollector = collectorActorNumber == NetworkServiceLocator.PlayerService.GetLocalActorNumber();
            inst.StartCoroutine(
                ExperienceMagnetEffect.FlyAllBallsToCollector(collector, isCollector, delay, speed));
        }

        // ── 实例方法 ──

        private void SpawnLocalPowerUp(uint itemId, Vector3 position, PowerUpDataSO data)
        {
            if (data == null)
            {
                Debug.LogWarning("[PowerUpManager] 道具数据为空，跳过生成");
                return;
            }

            var obj = poolManager?.GetFromPoolAndActivate(PoolConst.PowerUpItem, position);
            if (obj == null)
            {
                obj = Instantiate(powerUpPrefab, position, Quaternion.identity);
            }

            var powerUp = obj.GetComponent<PowerUpItemBase>();
            if (powerUp != null)
            {
                powerUp.SetupWithId(itemId, data);
                activePowerUps.Add(obj);
                activePowerUpsById[itemId] = obj;
                Debug.Log($"[PowerUpManager] 生成道具: {data.itemName} at {position}, itemId={itemId}");
            }
        }

        private void HideLocalPowerUp(uint itemId)
        {
            if (!activePowerUpsById.TryGetValue(itemId, out var obj))
            {
                // 道具可能已被拾取者本地回收，属正常情况
                return;
            }

            activePowerUpsById.Remove(itemId);
            activePowerUps.Remove(obj);

            if (poolManager != null)
            {
                poolManager.ReturnToPool(PoolConst.PowerUpItem, obj);
            }
            else
            {
                Destroy(obj);
            }

            Debug.Log($"[PowerUpManager] 道具已回收, itemId={itemId}");
        }
    }
}
