using System.Collections;
using System.Collections.Generic;
using Enemies;
using Photon.Pun;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// 怪物对象池管理器
    /// 用法：
    ///   生成怪物：MonsterPoolManager.instance.Spawn(MonsterPoolConst.Skeleton, position);
    ///   回收怪物：MonsterPoolManager.instance.Despawn(MonsterPoolConst.Skeleton, gameObject);
    /// </summary>
    public class MonsterPoolManager : MonoBehaviourPunCallbacks, IPunPrefabPool
    {
        // ===================== 单例 =====================
        public static MonsterPoolManager instance;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
                Destroy(gameObject);
        }

        // ===================== Inspector 配置 =====================
        [System.Serializable]
        public class PoolConfig
        {
            [Tooltip("与 MonsterPoolConst 中常量保持一致")]
            public string poolName;
            public GameObject prefab;

            [Tooltip("游戏启动时预创建的数量")]
            public int preloadCount = 5;
        }

        [Header("怪物池配置（在 Inspector 中添加）")]
        [SerializeField]
        private List<PoolConfig> poolConfigs = new List<PoolConfig>();

        // ===================== 内部数据 =====================
        private Dictionary<string, Queue<GameObject>> pools =
            new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        private bool photonReady = false;

        // ===================== 初始化 =====================
        void Start()
        {
            PhotonNetwork.PrefabPool = this;

            if (GameManager.isTest || (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom))
            {
                InitPools();
                photonReady = true;
            }
        }

        void Update()
        {
            // 等待 Photon 就绪后再初始化（仅网络模式）
            if (
                !photonReady
                && !GameManager.isTest
                && PhotonNetwork.IsConnectedAndReady
                && PhotonNetwork.InRoom
            )
            {
                InitPools();
                photonReady = true;
            }
        }

        private void InitPools()
        {
            foreach (var cfg in poolConfigs)
            {
                if (cfg.prefab == null)
                {
                    Debug.LogWarning($"[MonsterPool] {cfg.poolName} 未配置预制体");
                    continue;
                }
                prefabs[cfg.poolName] = cfg.prefab;
                pools[cfg.poolName] = new Queue<GameObject>();
                Preload(cfg.poolName, cfg.preloadCount);
            }
            Debug.Log("[MonsterPool] 初始化完成");
        }

        // ===================== 公共接口 =====================

        /// <summary>生成怪物，自动处理测试模式与网络模式</summary>
        public GameObject Spawn(string poolName, Vector3 position, Quaternion rotation = default)
        {
            if (rotation == default)
                rotation = Quaternion.identity;

            if (GameManager.isTest)
                return GetFromPool(poolName, position, rotation);

            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[MonsterPool] 只有房主可以生成怪物");
                return null;
            }
            return PhotonNetwork.Instantiate(poolName, position, rotation);
        }

        /// <summary>回收怪物，支持延迟回收</summary>
        public void Despawn(string poolName, GameObject monster, float delay = 0f)
        {
            if (monster == null)
                return;
            if (delay > 0f)
                StartCoroutine(DespawnDelay(poolName, monster, delay));
            else
                DespawnNow(poolName, monster);
        }

        /// <summary>回收该池内所有激活中的怪物</summary>
        public void DespawnAll(string poolName)
        {
            if (!GameManager.isTest && !PhotonNetwork.IsMasterClient)
                return;
            if (!prefabs.TryGetValue(poolName, out var prefab))
                return;

            foreach (var obj in FindObjectsOfType<EnemyBase>())
            {
                if (obj != null && obj.gameObject.activeSelf && obj.name.Contains(prefab.name))
                    DespawnNow(poolName, obj.gameObject);
            }
        }

        /// <summary>回收所有池的怪物</summary>
        public void DespawnAll()
        {
            foreach (var poolName in new List<string>(prefabs.Keys))
                DespawnAll(poolName);
        }

        // ===================== IPunPrefabPool 实现（Photon 回调） =====================

        /// <summary>Photon 调用：必须返回未激活的对象，Photon 自行激活</summary>
        public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
        {
            // 怪物走对象池
            if (prefabs.ContainsKey(prefabId))
                return GetFromPool(prefabId, position, rotation, activateOnGet: false);

            // 非怪物（如玩家）回退到默认 Resources 加载
            var prefab = Resources.Load<GameObject>(prefabId);
            if (prefab != null)
                return Object.Instantiate(prefab, position, rotation);

            return null;
        }

        public void Destroy(GameObject go)
        {
            string poolName = FindPoolName(go);
            if (string.IsNullOrEmpty(poolName))
            {
                Object.Destroy(go);
                return;
            }
            ReturnToPool(poolName, go);
        }

        // ===================== 内部方法 =====================

        private IEnumerator DespawnDelay(string poolName, GameObject monster, float delay)
        {
            yield return new WaitForSeconds(delay);
            DespawnNow(poolName, monster);
        }

        private void DespawnNow(string poolName, GameObject monster)
        {
            // 无论测试模式还是网络模式，都直接本地回池
            // 网络模式下不使用 PhotonNetwork.Destroy，因为它会先销毁子对象的网络组件，
            // 导致子对象在入池前就“消失”
            ReturnToPool(poolName, monster);
        }

        public GameObject GetFromPool(
            string poolName,
            Vector3 position,
            Quaternion rotation,
            bool activateOnGet = true
        )
        {
            // 从池中取出可用对象
            if (pools.TryGetValue(poolName, out var pool))
            {
                while (pool.Count > 0)
                {
                    var obj = pool.Dequeue();
                    if (obj == null)
                        continue;
                    obj.transform.SetPositionAndRotation(position, rotation);
                    // 取出时重新启用 PhotonView（预加载时禁用过）
                    var photonView = obj.GetComponent<PhotonView>();
                    if (photonView != null)
                        photonView.enabled = true;
                    if (activateOnGet)
                        obj.SetActive(true);
                    return obj;
                }
            }

            // 池空了则动态创建（始终返回未激活状态，让调用方决定）
            if (prefabs.TryGetValue(poolName, out var prefab))
            {
                var newObj = Object.Instantiate(prefab, position, rotation);
                newObj.name = poolName;
                newObj.SetActive(false);
                if (activateOnGet)
                    newObj.SetActive(true);
                return newObj;
            }

            Debug.LogError($"[MonsterPool] 未找到预制体: {poolName}");
            return null;
        }

        private void ReturnToPool(string poolName, GameObject monster)
        {
            if (monster == null)
                return;

            // 重置状态
            monster.SetActive(false);
            monster.name = poolName; // 统一标记名称，方便 FindPoolName 匹配
            monster.transform.SetParent(transform);
            monster.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            var rb = monster.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            monster.GetComponent<EnemyBase>()?.ResetState();

            if (!pools.ContainsKey(poolName))
                pools[poolName] = new Queue<GameObject>();
            pools[poolName].Enqueue(monster);
        }

        private void Preload(string poolName, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = Object.Instantiate(prefabs[poolName], Vector3.zero, Quaternion.identity);
                obj.name = poolName;
                ReturnToPool(poolName, obj);
            }
            Debug.Log($"[MonsterPool] 预创建 {poolName} x{count}");
        }

        /// <summary>通过对象名称反查所属池（回收时已统一设为 poolName）</summary>
        private string FindPoolName(GameObject obj)
        {
            // 优先精确匹配：回收时已将 name 设为 poolName
            if (pools.ContainsKey(obj.name))
                return obj.name;

            // 备用模糊匹配：应对网络模式下 Photon 返回的对象（名称可能带 (Clone) 等后缀）
            foreach (var kvp in prefabs)
            {
                if (obj.name.Contains(kvp.Value.name) || obj.name.Contains(kvp.Key))
                    return kvp.Key;
            }

            Debug.LogWarning($"[MonsterPool] FindPoolName 失败，无法识别对象: {obj.name}");
            return null;
        }

        // ===================== 调试工具 =====================

        public int GetPoolCount(string poolName) =>
            pools.TryGetValue(poolName, out var pool) ? pool.Count : 0;

        // ===================== 销毁清理 =====================

        void OnDestroy()
        {
            foreach (var pool in pools.Values)
                while (pool.Count > 0)
                {
                    var obj = pool.Dequeue();
                    if (obj)
                        Object.Destroy(obj);
                }

            pools.Clear();
            prefabs.Clear();
        }
    }

    /// <summary>
    /// 怪物对象池名称常量
    /// 与 Inspector 中 PoolConfig.poolName 保持一致
    /// </summary>
    public static class MonsterPoolConst
    {
        public const string Skeleton = "SkeletonPool";
    }
}
