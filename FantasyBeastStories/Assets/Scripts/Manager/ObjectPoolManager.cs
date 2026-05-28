using System.Collections.Generic;
using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;

namespace Manager
{
    public class ObjectPoolManager : MonoBehaviour
    {
        public static ObjectPoolManager instance;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private Dictionary<string, List<GameObject>> objectPools =
            new Dictionary<string, List<GameObject>>();
        private Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

        [SerializeField]
        private GameObject testPrefab;

        [SerializeField]
        private GameObject ImpactCannonCommonPrefab;

        [SerializeField]
        private GameObject ImpactCannonHitCommonPrefab;

        [SerializeField]
        private GameObject ImpactCannonTriggerPrefab;

        [SerializeField]
        private GameObject FireFirePrefab;

        [SerializeField]
        private GameObject DamageNumPrefab;

        private const string ImpactCannonPath = "FX/ImpactCannon/";
        private bool isPhotonReady = false;

        void Start()
        {
            if (GameManager.isTest)
            {
                InitializePool();
                return;
            }

            if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
            {
                InitializePool();
                isPhotonReady = true;
            }
            else
            {
                Debug.LogWarning("等待Photon连接");
            }
        }

        void Update()
        {
            if (GameManager.isTest)
                return;
            if (!isPhotonReady && PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
            {
                InitializePool();
                isPhotonReady = true;
            }
        }

        private void InitializePool()
        {
            // 缓存预制体引用
            CachePrefab("TestPool", testPrefab);
            CachePrefab("ImpactCannonCommonPool", ImpactCannonCommonPrefab);
            CachePrefab("ImpactCannonTriggerPool", ImpactCannonTriggerPrefab);
            CachePrefab("ImpactCannonHitCommonPool", ImpactCannonHitCommonPrefab);
            CachePrefab("DamageNumPool", DamageNumPrefab);
            AddMultipleToPool("ImpactCannonCommonPool", ImpactCannonCommonPrefab, 10);
            AddMultipleToPool("ImpactCannonHitCommonPool", ImpactCannonHitCommonPrefab, 20);
            AddMultipleToPool("ImpactCannonTriggerPool", ImpactCannonTriggerPrefab, 10);
            AddMultipleToPool("DamageNumPool", DamageNumPrefab, 100);
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
        /// ===== 核心改动：统一使用 Instantiate，不再用 PhotonNetwork.Instantiate =====
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
        /// 创建新对象（普通 Instantiate，不经过 Photon 网络）
        /// </summary>
        private GameObject CreateNewObject(string poolName, GameObject prefab)
        {
            // ===== 关键：全部使用普通 Instantiate =====
            // 这样生成的对象没有 PhotonView，不会被 Photon 自动同步
            GameObject obj = Instantiate(prefab, transform.position, Quaternion.identity);

            if (obj == null)
            {
                Debug.LogError($"无法实例化对象 '{poolName}'，预制体: {prefab?.name ?? "null"}");
                return null;
            }

            // 命名方便调试
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
    }

    public class ObjectPoolConst
    {
        public const string ImpactCannonCommonPool = "ImpactCannonCommonPool";
        public const string ImpactCannonHitCommonPool = "ImpactCannonHitCommonPool";
        public const string TestPool = "TestPool";
        public const string ImpactCannonTriggerPool = "ImpactCannonTriggerPool";
        public const string FireFirePool = "FireFirePool";
        public const string DamageNumPool = "DamageNumPool";
    }
}
