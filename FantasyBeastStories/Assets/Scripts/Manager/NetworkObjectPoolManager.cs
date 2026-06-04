using System.Collections;
using System.Collections.Generic;
using Enemies;
using Items;
using Photon.Pun;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// 统一网络对象池管理器（怪物 + 掉落物）
    /// 用法：
    ///   生成怪物：NetworkObjectPoolManager.instance.Spawn(NetworkObjectPoolConst.Skeleton, position);
    ///   回收怪物：NetworkObjectPoolManager.instance.Despawn(NetworkObjectPoolConst.Skeleton, gameObject);
    ///   生成掉落物：NetworkObjectPoolManager.instance.Spawn(NetworkObjectPoolConst.ExperienceBall_Blue, position);
    ///   回收掉落物：NetworkObjectPoolManager.instance.Despawn(NetworkObjectPoolConst.ExperienceBall_Blue, gameObject);
    /// </summary>
    public class NetworkObjectPoolManager : MonoBehaviourPunCallbacks, IPunPrefabPool
    {
        #region 单例模式

        public static NetworkObjectPoolManager instance;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
                Destroy(gameObject);
        }
        #endregion

        // ===================== Inspector 配置 =====================
        [System.Serializable]
        public class PoolConfig
        {
            [Tooltip("与 NetworkObjectPoolConst 中常量保持一致")]
            public string poolName;
            public GameObject prefab;

            [Tooltip("游戏启动时预创建的数量")]
            public int preloadCount = 5;
        }

        [Header("网络对象池配置（在 Inspector 中添加）")]
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
                    Debug.LogWarning($"[NetworkObjectPool] {cfg.poolName} 未配置预制体");
                    continue;
                }
                prefabs[cfg.poolName] = cfg.prefab;
                pools[cfg.poolName] = new Queue<GameObject>();
                Preload(cfg.poolName, cfg.preloadCount);
            }
            Debug.Log("[NetworkObjectPool] 初始化完成");
        }

        // ===================== 公共接口 =====================

        /// <summary>生成对象，自动处理测试模式与网络模式</summary>
        public GameObject Spawn(string poolName, Vector3 position, Quaternion rotation = default)
        {
            if (rotation == default)
                rotation = Quaternion.identity;

            if (GameManager.isTest)
                return GetFromPool(poolName, position, rotation);

            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[NetworkObjectPool] 只有房主可以生成网络对象");
                return null;
            }
            return PhotonNetwork.Instantiate(poolName, position, rotation);
        }

        /// <summary>回收对象，支持延迟回收</summary>
        public void Despawn(string poolName, GameObject obj, float delay = 0f)
        {
            if (obj == null)
                return;
            if (delay > 0f)
                StartCoroutine(DespawnDelay(poolName, obj, delay));
            else
                DespawnNow(poolName, obj);
        }

        /// <summary>回收该池内所有激活中的怪物</summary>
        public void DespawnAll(string poolName)
        {
            if (!GameManager.isTest && !PhotonNetwork.IsMasterClient)
                return;
            if (!prefabs.TryGetValue(poolName, out var prefab))
                return;

            // 尝试匹配怪物
            foreach (var enemy in FindObjectsOfType<EnemyBase>())
            {
                if (
                    enemy != null
                    && enemy.gameObject.activeSelf
                    && enemy.name.Contains(prefab.name)
                )
                    DespawnNow(poolName, enemy.gameObject);
            }
            // 尝试匹配掉落物
            foreach (var item in FindObjectsOfType<DropItemBase>())
            {
                if (item != null && item.gameObject.activeSelf && item.name.Contains(prefab.name))
                    DespawnNow(poolName, item.gameObject);
            }
        }

        /// <summary>回收所有池的对象</summary>
        public void DespawnAll()
        {
            foreach (var poolName in new List<string>(prefabs.Keys))
                DespawnAll(poolName);
        }

        // ===================== IPunPrefabPool 实现（Photon 回调） =====================

        /// <summary>Photon 调用：必须返回未激活的对象，Photon 自行激活</summary>
        public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
        {
            // 库内对象（怪物/掉落物）走对象池
            if (prefabs.ContainsKey(prefabId))
                return GetFromPool(prefabId, position, rotation, activateOnGet: false);

            // 非库内对象（如玩家）回退到默认 Resources 加载
            var prefab = Resources.Load<GameObject>(prefabId);
            if (prefab != null)
                return Object.Instantiate(prefab, position, rotation);

            Debug.LogError(
                $"[NetworkObjectPool] 无法实例化: {prefabId}，未在对象池或 Resources 中找到"
            );
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

        private IEnumerator DespawnDelay(string poolName, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            DespawnNow(poolName, obj);
        }

        private void DespawnNow(string poolName, GameObject obj)
        {
            // 无论测试模式还是网络模式，都直接本地回池
            // 网络模式下不使用 PhotonNetwork.Destroy，因为它会先销毁子对象的网络组件
            ReturnToPool(poolName, obj);
        }

        private GameObject GetFromPool(
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

            // 池空了则动态创建
            if (prefabs.TryGetValue(poolName, out var prefab))
            {
                //创建对象并且添加到对象池
                var newObj = Object.Instantiate(prefab, position, rotation);
                newObj.name = poolName;
                newObj.SetActive(false);
                // ✅ 不要放回池子，直接返回使用
                // ✅ 尊重 activateOnGet 参数
                if (activateOnGet)
                    newObj.SetActive(true);
                else
                    newObj.SetActive(false);
                return newObj;
            }

            Debug.LogError($"[NetworkObjectPool] 未找到预制体: {poolName}");
            return null;
        }

        private void ReturnToPool(string poolName, GameObject obj)
        {
            if (obj == null)
                return;

            // 重置对应组件状态
            obj.GetComponent<EnemyBase>()?.ResetState();
            obj.GetComponent<DropItemBase>()?.ResetState();
            obj.SetActive(false);
            obj.name = poolName; // 统一标记名称，方便 FindPoolName 匹配
            obj.transform.SetParent(transform);
            obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            var rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (!pools.ContainsKey(poolName))
                pools[poolName] = new Queue<GameObject>();
            pools[poolName].Enqueue(obj);
        }

        private void Preload(string poolName, int count)
        {
            for (int i = 0; i < count; i++)
            {
                // 预加载时先禁用 PhotonView，防止本地 Instantiate 触发 Photon 网络销毁流程
                var obj = Object.Instantiate(prefabs[poolName], Vector3.zero, Quaternion.identity);
                obj.name = poolName;
                var photonView = obj.GetComponent<PhotonView>();
                if (photonView != null)
                    photonView.enabled = false;
                ReturnToPool(poolName, obj);
            }
            Debug.Log($"[NetworkObjectPool] 预创建 {poolName} x{count}");
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

            Debug.LogWarning($"[NetworkObjectPool] FindPoolName 失败，无法识别对象: {obj.name}");
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
    /// 网络对象池名称常量
    /// 与 Inspector 中 PoolConfig.poolName 保持一致
    /// </summary>
    public static class NetworkObjectPoolConst
    {
        // 掉落物
        public const string ExperienceBall_Blue = "ExperienceBall_BluePool";

        // 怪物
        public const string Skeleton = "SkeletonPool";
    }
}
