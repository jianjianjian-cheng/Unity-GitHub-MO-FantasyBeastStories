using System.Collections.Generic;
using UnityEngine;
using Controllers.PowerUp;
using Core;
using Controllers.Services;
using Managers;
using Controllers.Item;
using Photon.Pun;

namespace Controllers.PowerUp
{
    /// <summary>
    /// 道具管理器 - 单例服务
    /// 职责：道具生成、生命周期管理、随机掉落
    /// </summary>
    public class PowerUpManager : MonoBehaviour, IPowerUpService
    {
        public static PowerUpManager Instance { get; private set; }

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

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            poolManager = ServiceLocator.Get<ObjectPoolManager>();
            DomainServiceLocator.Register<IPowerUpService>(this);
        }

        private void Update()
        {
            if (!autoSpawn) return;

            spawnTimer += UnityEngine.Time.deltaTime;
            if (spawnTimer >= spawnTimer && activePowerUps.Count < maxActivePowerUps)
            {
                SpawnRandomPowerUp(GetRandomPosition());
                spawnTimer = 0f;
            }
        }

        public void SpawnPowerUp(PowerUpDataSO data, Vector3 position)
        {
            if (data == null || powerUpPrefab == null)
            {
                Debug.LogError("[PowerUpManager] 缺少必要配置！");
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
                powerUp.Setup(data);
                activePowerUps.Add(obj);
                Debug.Log($"[PowerUpManager] 生成道具: {data.itemName} at {position}");
            }
        }

        public void SpawnRandomPowerUp(Vector3 position)
        {
            if (availablePowerUps.Count == 0) return;

            var randomData = GetWeightedRandom();
            SpawnPowerUp(randomData, position);
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
            Debug.Log("[PowerUpManager] 清除所有道具");
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

        public void RemoveFromActiveList(GameObject obj)
        {
            activePowerUps.Remove(obj);
        }

        /// <summary>
        /// RPC回调：处理道具被拾取的网络同步
        /// </summary>
        public static void HandleCollectPowerUpRPC(int viewId)
        {
            if (Instance == null)
            {
                Debug.LogWarning("[PowerUpManager] 实例不存在，无法处理RPC");
                return;
            }

            PhotonView pv = PhotonView.Find(viewId);
            if (pv != null)
            {
                var powerUp = pv.GetComponent<PowerUpItemBase>();
                if (powerUp != null)
                {
                    Instance.activePowerUps.Remove(powerUp.gameObject);
                    Instance.poolManager?.ReturnToPool(PoolConst.PowerUpItem, powerUp.gameObject);
                    Debug.Log($"[PowerUpManager] RPC同步：道具已回收，ViewID={viewId}");
                }
            }
        }
    }
}