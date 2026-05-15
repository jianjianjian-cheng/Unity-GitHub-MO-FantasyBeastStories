using System.Collections.Generic;
using Enemies;
using Photon.Pun;
using UnityEngine;

namespace Manager
{
    public class MonsterPoolManager : MonoBehaviour
    {
        #region 单例模式
        public static MonsterPoolManager instance;

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
        #endregion

        private Dictionary<string, List<GameObject>> monsterPools = new Dictionary<string, List<GameObject>>();
        private Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

        [Header("怪物预制体")]
        [SerializeField] private GameObject SkeletonPrefab;
        private bool isPhotonReady = false;

        // 怪物初始池大小配置
        [System.Serializable]
        public class MonsterPoolConfig
        {
            public string poolName;
            public GameObject prefab;
            public int initialSize;
        }

        [Header("怪物池配置")]
        [SerializeField] private List<MonsterPoolConfig> monsterConfigs = new List<MonsterPoolConfig>();

        void Start()
        {
            if (GameManager.isTest)
            {
                InitializeAllPools();
                return;
            }

            if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
            {
                InitializeAllPools();
                isPhotonReady = true;
            }
            else
            {
                Debug.LogWarning("[MonsterPool] 等待Photon连接");
            }
        }

        void Update()
        {
            if (GameManager.isTest) return;
            if (!isPhotonReady && PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
            {
                InitializeAllPools();
                isPhotonReady = true;
            }
        }

        private void InitializeAllPools()
        {
            // 缓存预制体引用
            CachePrefab("SkeletonPool", SkeletonPrefab);

            // 根据配置初始化怪物池
            foreach (var config in monsterConfigs)
            {
                if (config.prefab != null)
                {
                    CachePrefab(config.poolName, config.prefab);
                    AddMultipleToPool(config.poolName, config.prefab, config.initialSize);
                }
            }

            // 如果没有配置，使用默认初始化
            if (monsterConfigs.Count == 0)
            {
                AddMultipleToPool("SkeletonPool", SkeletonPrefab, 10);
            }

            Debug.Log("[MonsterPool] 所有怪物池初始化完成");
        }

        private void CachePrefab(string key, GameObject prefab)
        {
            if (prefab != null && !prefabCache.ContainsKey(key))
            {
                prefabCache[key] = prefab;
            }
        }

        /// <summary>
        /// 生成怪物（从池中获取并激活）
        /// </summary>
        public GameObject SpawnMonster(string poolName, Vector3 spawnPosition, Quaternion? spawnRotation = null)
        {
            GameObject monster = GetFromPoolAndActivate(poolName, spawnPosition);

            if (monster != null)
            {
                // 设置朝向
                if (spawnRotation.HasValue)
                {
                    monster.transform.rotation = spawnRotation.Value;
                }
                else
                {
                    monster.transform.rotation = Quaternion.identity;
                }
            }

            return monster;
        }

        /// <summary>
        /// 回收怪物（死亡后返回对象池）
        /// </summary>
        public void DespawnMonster(string poolName, GameObject monster, float delay = 0f)
        {
            if (monster == null)
            {
                Debug.LogWarning($"[MonsterPool] 尝试回收空怪物对象");
                return;
            }

            if (delay > 0f)
            {
                StartCoroutine(DespawnAfterDelay(poolName, monster, delay));
            }
            else
            {
                ReturnToPool(poolName, monster);
            }
        }

        private System.Collections.IEnumerator DespawnAfterDelay(string poolName, GameObject monster, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnToPool(poolName, monster);
        }

        /// <summary>
        /// 回收所有激活的怪物（场景切换时使用）
        /// </summary>
        public void DespawnAllMonsters(string poolName)
        {
            if (monsterPools.TryGetValue(poolName, out var pool))
            {
                foreach (var monster in pool)
                {
                    if (monster != null && monster.activeSelf)
                    {
                        ReturnToPool(poolName, monster);
                    }
                }
                Debug.Log($"[MonsterPool] 回收池 '{poolName}' 中所有激活的怪物");
            }
        }

        /// <summary>
        /// 回收所有池中的所有激活怪物
        /// </summary>
        public void DespawnAllMonsters()
        {
            foreach (var poolName in monsterPools.Keys)
            {
                DespawnAllMonsters(poolName);
            }
            Debug.Log("[MonsterPool] 回收所有怪物");
        }

        /// <summary>
        /// 从对象池获取对象并激活
        /// </summary>
        private GameObject GetFromPoolAndActivate(string poolName, Vector3? position = null)
        {
            if (monsterPools.TryGetValue(poolName, out List<GameObject> pool))
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
                    GameObject newObj = CreateNewMonster(poolName, prefab);
                    if (newObj != null)
                    {
                        pool.Add(newObj);
                        newObj.SetActive(true);
                        if (position.HasValue)
                            newObj.transform.position = position.Value;
                        Debug.LogWarning($"[MonsterPool] 怪物池 '{poolName}' 动态扩容，当前数量: {pool.Count}");
                        return newObj;
                    }
                }

                Debug.LogError($"[MonsterPool] 怪物池 '{poolName}' 没有可用对象且无法扩容");
            }
            else
            {
                Debug.LogError($"[MonsterPool] 怪物池 '{poolName}' 不存在");
            }
            return null;
        }

        /// <summary>
        /// 将怪物返回对象池
        /// </summary>
        private void ReturnToPool(string poolName, GameObject monster)
        {
            if (monster == null) return;

            if (monsterPools.TryGetValue(poolName, out var pool) && pool.Contains(monster))
            {
                // 禁用并重置怪物
                monster.SetActive(false);
                monster.transform.SetParent(transform);
                monster.transform.localPosition = Vector3.zero;
                monster.transform.localRotation = Quaternion.identity;

                // 重置 Rigidbody（如果有）
                Rigidbody rb = monster.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                // 重置血量等状态在 MonsterBase.OnDespawned() 中处理

                Debug.Log($"[MonsterPool] 回收怪物: {poolName}");
            }
            else
            {
                Debug.LogWarning($"[MonsterPool] 怪物 '{monster.name}' 不属于池 '{poolName}'，直接销毁");
                Destroy(monster);
            }
        }

        /// <summary>
        /// 添加多个怪物到对象池
        /// </summary>
        public void AddMultipleToPool(string poolName, GameObject prefab, int count)
        {
            if (prefab == null)
            {
                Debug.LogError($"[MonsterPool] 预制体 '{poolName}' 为空，无法添加到怪物池");
                return;
            }

            if (!monsterPools.ContainsKey(poolName))
            {
                monsterPools[poolName] = new List<GameObject>();
            }

            int currentCount = monsterPools[poolName].Count;

            for (int i = 0; i < count; i++)
            {
                GameObject monster = CreateNewMonster(poolName, prefab);
                if (monster != null)
                {
                    monster.transform.SetParent(transform);
                    monster.SetActive(false);
                    monsterPools[poolName].Add(monster);
                }
            }

            Debug.Log($"[MonsterPool] 怪物池 '{poolName}' 添加 {count} 个对象，总计: {monsterPools[poolName].Count}");
        }

        /// <summary>
        /// 创建新怪物实例
        /// </summary>
        private GameObject CreateNewMonster(string poolName, GameObject prefab)
        {
            if (GameManager.isTest)
            {
                GameObject monster_Test = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                // 命名方便调试
                monster_Test.name = $"{prefab.name}_Pooled_{monsterPools[poolName].Count}";
                return monster_Test;
            }
            GameObject monster = PhotonNetwork.Instantiate(prefab.name, Vector3.zero, Quaternion.identity);

            if (monster == null)
            {
                Debug.LogError($"[MonsterPool] 无法实例化怪物 '{poolName}'");
                return null;
            }

            // 命名方便调试
            monster.name = $"{prefab.name}_Pooled_{monsterPools[poolName].Count}";
            return monster;
        }

        /// <summary>
        /// 预加载怪物池（在场景加载时调用）
        /// </summary>
        public void PreloadPool(string poolName, int targetCount)
        {
            if (monsterPools.TryGetValue(poolName, out var pool))
            {
                int needCount = targetCount - pool.Count;
                if (needCount > 0)
                {
                    if (prefabCache.TryGetValue(poolName, out GameObject prefab))
                    {
                        AddMultipleToPool(poolName, prefab, needCount);
                    }
                }
            }
            else if (prefabCache.TryGetValue(poolName, out GameObject prefab))
            {
                AddMultipleToPool(poolName, prefab, targetCount);
            }
        }

        /// <summary>
        /// 清空指定怪物池
        /// </summary>
        public void ClearMonsterPool(string poolName)
        {
            if (monsterPools.ContainsKey(poolName))
            {
                foreach (var monster in monsterPools[poolName])
                {
                    if (monster != null) Destroy(monster);
                }
                monsterPools[poolName].Clear();
                Debug.Log($"[MonsterPool] 清空怪物池: {poolName}");
            }
        }

        /// <summary>
        /// 清空所有怪物池
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var poolName in monsterPools.Keys)
            {
                ClearMonsterPool(poolName);
            }
            monsterPools.Clear();
            Debug.Log("[MonsterPool] 清空所有怪物池");
        }

        // ===== 调试方法 =====

        public int GetPoolCount(string poolName)
        {
            if (monsterPools.TryGetValue(poolName, out var pool))
            {
                return pool.Count;
            }
            return 0;
        }

        public int GetActiveCount(string poolName)
        {
            if (monsterPools.TryGetValue(poolName, out var pool))
            {
                int count = 0;
                foreach (var obj in pool)
                {
                    if (obj != null && obj.activeSelf) count++;
                }
                return count;
            }
            return 0;
        }

        public Dictionary<string, int> GetAllPoolStats()
        {
            Dictionary<string, int> stats = new Dictionary<string, int>();
            foreach (var poolName in monsterPools.Keys)
            {
                stats[poolName] = GetActiveCount(poolName);
            }
            return stats;
        }
    }

    /// <summary>
    /// 怪物对象池常量
    /// </summary>
    public class MonsterPoolConst
    {

        public const string SkeletonPool = "SkeletonPool";

    }
}