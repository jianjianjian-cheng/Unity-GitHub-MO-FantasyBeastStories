using System.Collections.Generic;
using UnityEngine;
using Application;
using Domain.Event;
using Domain.Event.Channels.General;
using Domain.Services;

namespace Domain.Pool
{
    public class ObjectPoolManager : MonoBehaviour
    {
        [SerializeField]
        private PoolConfigSO poolConfig;

        void Awake()
        {
            ServiceLocator.Register(this);
            DomainServiceLocator.Register(this);
            DontDestroyOnLoad(gameObject);
        }

        void OnEnable()
        {
            EventChannelLocator.MainContainer.poolOperationChannel.RegisterListener(OnPoolOperation);
        }

        void OnDisable()
        {
            EventChannelLocator.MainContainer.poolOperationChannel.UnregisterListener(OnPoolOperation);
        }

        private void OnPoolOperation(PoolOperationData data)
        {
            switch (data.operationType)
            {
                case PoolOperationType.GetFromPoolAndActivate:
                    var obj = GetFromPoolAndActivate(data.poolName, data.position);
                    data.resultCallback?.Invoke(obj);
                    break;
                case PoolOperationType.ReturnToPool:
                    ReturnToPool(data.poolName, data.targetObject);
                    break;
                case PoolOperationType.AddMultipleToPool:
                    AddMultipleToPool(data.poolName, data.prefab, data.count);
                    break;
                case PoolOperationType.GetPoolCount:
                    var count = GetPoolCount(data.poolName);
                    data.countCallback?.Invoke(count);
                    break;
            }
        }

        private Dictionary<string, List<GameObject>> objectPools =
            new Dictionary<string, List<GameObject>>();
        private Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

        private bool isPoolInitialized = false;

        void Start()
        {
            if (poolConfig == null)
            {
                poolConfig = Resources.Load<PoolConfigSO>("Config/PoolConfig");
            }

            if (EventChannelLocator.MainContainer.gameSettings.IsTest)
            {
                InitializePool();
                return;
            }

            if (NetworkServiceLocator.PlayerService.IsConnectedAndInRoom)
            {
                InitializePool();
                isPoolInitialized = true;
            }
            else
            {
                Debug.LogWarning("等待网络连接就绪后初始化对象池");
            }
        }

        void Update()
        {
            if (EventChannelLocator.MainContainer.gameSettings.IsTest)
                return;
            if (!isPoolInitialized && NetworkServiceLocator.PlayerService.IsConnectedAndInRoom)
            {
                InitializePool();
                isPoolInitialized = true;
            }
        }

        private void InitializePool()
        {
            if (poolConfig == null || poolConfig.pools == null)
            {
                Debug.LogError("[ObjectPoolManager] PoolConfigSO 未配置");
                return;
            }

            foreach (var entry in poolConfig.pools)
            {
                if (entry == null || entry.prefab == null)
                {
                    Debug.LogWarning($"[ObjectPoolManager] 跳过无效的池配置: {entry?.poolName ?? "null"}");
                    continue;
                }

                CachePrefab(entry.poolName, entry.prefab);

                if (entry.preloadCount > 0)
                {
                    AddMultipleToPool(entry.poolName, entry.prefab, entry.preloadCount);
                }
            }
        }

        private void CachePrefab(string key, GameObject prefab)
        {
            if (prefab != null && !prefabCache.ContainsKey(key))
            {
                prefabCache[key] = prefab;
            }
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public void ClearPool(string poolName)
        {
            if (objectPools.ContainsKey(poolName))
            {
                foreach (var obj in objectPools[poolName])
                {
                    if (obj != null)
                        Destroy(obj);
                }
                objectPools[poolName].Clear();
            }
        }

        /// <summary>
        /// 从对象池获取对象并激活
        /// </summary>
        public GameObject GetFromPoolAndActivate(string poolName, Vector3? position = null)
        {
            if (objectPools.TryGetValue(poolName, out List<GameObject> pool))
            {
                // 先找未激活的对象
                foreach (var obj in pool)
                {
                    if (obj != null && !obj.activeSelf)
                    {
                        obj.SetActive(true);
                        if (position.HasValue)
                            obj.transform.position = position.Value;
                        return obj;
                    }
                }

                // 池子里没有可用对象，动态扩容
                if (prefabCache.TryGetValue(poolName, out GameObject prefab) && prefab != null)
                {
                    GameObject newObj = CreateNewObject(poolName, prefab);
                    if (newObj != null)
                    {
                        pool.Add(newObj);
                        newObj.SetActive(true);
                        if (position.HasValue)
                            newObj.transform.position = position.Value;
                        Debug.Log($"对象池 '{poolName}' 动态扩容，当前数量: {pool.Count}");
                        return newObj;
                    }
                }

                Debug.LogWarning($"对象池 '{poolName}' 没有可用对象且无法扩容");
            }
            else
            {
                Debug.LogWarning($"对象池 '{poolName}' 不存在");
            }
            return null;
        }

        /// <summary>
        /// 将对象返回对象池并禁用
        /// </summary>
        public void ReturnToPool(string poolName, GameObject obj)
        {
            if (obj == null)
                return;

            if (objectPools.TryGetValue(poolName, out var pool) && pool.Contains(obj))
            {
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;

                // 重置 Rigidbody（如果有）
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
            else
            {
                // 如果不在池子里，直接销毁
                Debug.LogWarning($"对象 '{obj.name}' 不属于对象池 '{poolName}'，直接销毁");
                Destroy(obj);
            }
        }

        /// <summary>
        /// 添加多个对象到对象池
        /// </summary>
        public void AddMultipleToPool(string poolName, GameObject prefab, int count)
        {
            if (prefab == null)
            {
                Debug.LogError($"预制体 '{poolName}' 为空，无法添加到对象池");
                return;
            }

            if (!objectPools.ContainsKey(poolName))
            {
                objectPools[poolName] = new List<GameObject>();
            }

            for (int i = 0; i < count; i++)
            {
                GameObject obj = CreateNewObject(poolName, prefab);
                if (obj != null)
                {
                    obj.transform.SetParent(transform);
                    obj.SetActive(false);
                    objectPools[poolName].Add(obj);
                }
            }

            Debug.Log($"对象池 '{poolName}' 初始化完成，共 {objectPools[poolName].Count} 个对象");
        }

        /// <summary>
        /// 创建新对象（普通 Instantiate，不经过网络同步）
        /// </summary>
        private GameObject CreateNewObject(string poolName, GameObject prefab)
        {
            GameObject obj = UnityEngine.Object.Instantiate(prefab, transform.position, Quaternion.identity);

            if (obj == null)
            {
                Debug.LogError($"无法实例化对象 '{poolName}'，预制体: {prefab?.name ?? "null"}");
                return null;
            }

            obj.name = $"{prefab.name}_Pooled";

            return obj;
        }

        /// <summary>
        /// 获取对象池中的对象数量（调试用）
        /// </summary>
        public int GetPoolCount(string poolName)
        {
            if (objectPools.TryGetValue(poolName, out var pool))
            {
                return pool.Count;
            }
            return 0;
        }

        /// <summary>
        /// 获取对象池中激活的对象数量（调试用）
        /// </summary>
        public int GetActiveCount(string poolName)
        {
            if (objectPools.TryGetValue(poolName, out var pool))
            {
                int count = 0;
                foreach (var obj in pool)
                {
                    if (obj != null && obj.activeSelf)
                        count++;
                }
                return count;
            }
            return 0;
        }

        public void DestroyAllPools()
        {
            foreach (var pool in objectPools.Values)
            {
                foreach (var obj in pool)
                {
                    Destroy(obj);
                }
            }
        }

        void OnDestroy()
        {
            DestroyAllPools();
        }
    }

    // ===== 向后兼容的常量引用（推荐使用 PoolConst） =====
    public static class ObjectPoolConst
    {
        public const string ImpactCannonCommonPool = PoolConst.ImpactCannonCommonPool;
        public const string ImpactCannonLightenPool = PoolConst.ImpactCannonLightenPool;
        public const string ImpactCannonHitCommonPool = PoolConst.ImpactCannonHitCommonPool;
        public const string ImpactCannonHitLightenPool = PoolConst.ImpactCannonHitLightenPool;
        public const string ImpactCannonWinterPool = PoolConst.ImpactCannonWinterPool;
        public const string ImpactCannonHitWinterPool = PoolConst.ImpactCannonHitWinterPool;
        public const string ImpactCannonGrassPool = PoolConst.ImpactCannonGrassPool;
        public const string ImpactCannonHitGrassPool = PoolConst.ImpactCannonHitGrassPool;
        public const string TestPool = PoolConst.TestPool;
        public const string ImpactCannonTriggerPool = PoolConst.ImpactCannonTriggerPool;
        public const string FireFirePool = PoolConst.FireFirePool;
        public const string DamageNumPool = PoolConst.DamageNumPool;
    }
}