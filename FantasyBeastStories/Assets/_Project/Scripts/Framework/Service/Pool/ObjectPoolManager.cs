using System.Collections.Generic;
using UnityEngine;
using Core;
using Core.Channels.General;
using Core.Contracts;
using Core.Network;

namespace Core
{
    public class ObjectPoolManager : MonoBehaviour
    {
        private PoolConfigSO poolConfig;
        private class PoolData
        {
            public List<GameObject> allObjects = new List<GameObject>();
            public HashSet<GameObject> allObjectsSet = new HashSet<GameObject>();
            public Queue<GameObject> available = new Queue<GameObject>();
        }

        private Dictionary<string, PoolData> objectPools = new Dictionary<string, PoolData>();
        private Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
        private Dictionary<GameObject, Rigidbody> rigidbodyCache = new Dictionary<GameObject, Rigidbody>();

        private bool isPoolInitialized = false;

        void Awake()
        {
            ServiceLocator.Register(this);
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
            // 仅处理本池管理器已注册的池，忽略 NetworkObjectPoolManager 的网络池事件
            if (!objectPools.ContainsKey(data.poolName) && !prefabCache.ContainsKey(data.poolName))
                return;

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

        void Start()
        {
            poolConfig = AssetLoader.LoadAsset<PoolConfigSO>("Local_Config_PoolConfig");

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
                if (entry == null || string.IsNullOrEmpty(entry.poolName))
                {
                    Debug.LogWarning("[ObjectPoolManager] 跳过无效的池配置");
                    continue;
                }

                // 优先通过 Addressables 加载预制体（确保热更生效）
                GameObject prefab = null;
                if (!string.IsNullOrEmpty(entry.addressableKey))
                {
                    prefab = AssetLoader.TryLoadAsset<GameObject>(entry.addressableKey);
                }

                // 回退到 Inspector 引用
                if (prefab == null)
                {
                    prefab = entry.prefab;
                }

                if (prefab == null)
                {
                    continue;
                }


                CachePrefab(entry.poolName, prefab);

                if (entry.preloadCount > 0)
                {
                    AddMultipleToPool(entry.poolName, prefab, entry.preloadCount);
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
            if (objectPools.TryGetValue(poolName, out var poolData))
            {
                foreach (var obj in poolData.allObjects)
                {
                    if (obj != null)
                    {
                        rigidbodyCache.Remove(obj);
                        Destroy(obj);
                    }
                }
                poolData.allObjects.Clear();
                poolData.allObjectsSet.Clear();
                poolData.available.Clear();
            }
        }

        /// <summary>
        /// 从对象池获取对象并激活（O(1) Queue 操作）
        /// </summary>
        public GameObject GetFromPoolAndActivate(string poolName, Vector3? position = null)
        {
            if (objectPools.TryGetValue(poolName, out PoolData poolData))
            {
                GameObject obj = null;

                // 从可用队列中取（O(1)）
                while (poolData.available.Count > 0)
                {
                    obj = poolData.available.Dequeue();
                    if (obj != null)
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
                        poolData.allObjects.Add(newObj);
                        poolData.allObjectsSet.Add(newObj);
                        newObj.SetActive(true);
                        if (position.HasValue)
                            newObj.transform.position = position.Value;
#if UNITY_EDITOR
                        Debug.Log($"对象池 '{poolName}' 动态扩容，当前数量: {poolData.allObjects.Count}");
#endif

                        var rb = newObj.GetComponent<Rigidbody>();
                        if (rb != null)
                            rigidbodyCache[newObj] = rb;

                        return newObj;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"对象池 '{poolName}' 不存在");
            }
            return null;
        }

        /// <summary>
        /// 将对象返回对象池并禁用（O(1) Queue 操作）
        /// </summary>
        public void ReturnToPool(string poolName, GameObject obj)
        {
            if (obj == null)
                return;

            if (objectPools.TryGetValue(poolName, out var poolData) && poolData.allObjectsSet.Contains(obj))
            {
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;

                // 重置 Rigidbody（如果有）— 使用缓存避免每次 GetComponent
                if (rigidbodyCache.TryGetValue(obj, out Rigidbody rb))
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                // 放回可用队列
                poolData.available.Enqueue(obj);
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
                objectPools[poolName] = new PoolData();
            }

            var poolData = objectPools[poolName];

            for (int i = 0; i < count; i++)
            {
                GameObject obj = CreateNewObject(poolName, prefab);
                if (obj != null)
                {
                    obj.transform.SetParent(transform);
                    obj.SetActive(false);
                    poolData.allObjects.Add(obj);
                    poolData.allObjectsSet.Add(obj);
                    poolData.available.Enqueue(obj);

                    var rb = obj.GetComponent<Rigidbody>();
                    if (rb != null)
                        rigidbodyCache[obj] = rb;
                }
            }

#if UNITY_EDITOR
            Debug.Log($"对象池 '{poolName}' 初始化完成，共 {poolData.allObjects.Count} 个对象");
#endif
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
            if (objectPools.TryGetValue(poolName, out var poolData))
            {
                return poolData.allObjects.Count;
            }
            return 0;
        }

        /// <summary>
        /// 获取对象池中激活的对象数量（调试用）
        /// </summary>
        public int GetActiveCount(string poolName)
        {
            if (objectPools.TryGetValue(poolName, out var poolData))
            {
                int count = 0;
                foreach (var obj in poolData.allObjects)
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
            foreach (var poolData in objectPools.Values)
            {
                foreach (var obj in poolData.allObjects)
                {
                    if (obj != null)
                    {
                        rigidbodyCache.Remove(obj);
                        Destroy(obj);
                    }
                }
                poolData.allObjects.Clear();
                poolData.allObjectsSet.Clear();
                poolData.available.Clear();
            }
            rigidbodyCache.Clear();
        }

        void OnDestroy()
        {
            DestroyAllPools();
        }
    }

    // ===== 之前快速原型的时候用的，暂时留着 =====
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