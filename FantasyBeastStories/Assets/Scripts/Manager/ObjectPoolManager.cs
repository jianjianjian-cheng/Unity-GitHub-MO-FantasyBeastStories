using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace Manager
{
    public class ObjectPoolManager : MonoBehaviour
    {
        //单例
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
        //对象池字典
        private Dictionary<string, List<GameObject>> objectPools = new Dictionary<string, List<GameObject>>();
        [SerializeField] private GameObject testPrefab; //测试预制体
        [SerializeField] private GameObject ImpactCannonCommonPrefab; // 火球预制体
        [SerializeField] private GameObject ImpactCannonHitCommonPrefab; // 火球击中效果预制体
        [SerializeField] private GameObject ImpactCannonTriggerPrefab; // 冲击炮Trigger预制体
        //冲击炮的路径
        private const string ImpactCannonPath = "FX/ImpactCannon/";
        private bool isPhotonReady = false; // Photon是否准备就绪
        [SerializeField] private bool isTest = false; // 是否测试模式
        void Start()
        {
            if (isTest)
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
            if (isTest) return;
            if (!isPhotonReady && PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
            {
                InitializePool();
                isPhotonReady = true;
            }
        }

        private void InitializePool()
        {
            if (isTest)
            {
                AddMultipleToPool("TestPool", testPrefab, 5);
                AddMultipleToPool("ImpactCannonCommonPool", ImpactCannonCommonPrefab, 10, ImpactCannonPath);
                AddMultipleToPool("ImpactCannonTriggerPool", ImpactCannonTriggerPrefab, 10, ImpactCannonPath);
                AddMultipleToPool("ImpactCannonHitCommonPool", ImpactCannonHitCommonPrefab, 20, ImpactCannonPath);
                return;
            }
            //添加到对象池
            AddMultipleToPool("ImpactCannonCommonPool", ImpactCannonCommonPrefab, 10, ImpactCannonPath);
            //添加冲击炮击中效果到对象池
            AddMultipleToPool("ImpactCannonHitCommonPool", ImpactCannonHitCommonPrefab, 20, ImpactCannonPath);
            //添加冲击炮Trigger到对象池
            AddMultipleToPool("ImpactCannonTriggerPool", ImpactCannonTriggerPrefab, 10, ImpactCannonPath);
        }
        //清空对象池
        public void ClearPool(string poolName)
        {
            if (objectPools.ContainsKey(poolName))
            {
                objectPools[poolName].Clear();
            }
        }
        //从对象池获取对象并激活
        public GameObject GetFromPoolAndActivate(string poolName, Vector3? position = null)
        {
            if (objectPools.TryGetValue(poolName, out List<GameObject> pool))
            {
                foreach (var obj in pool)
                {
                    if (!obj.activeSelf)
                    {
                        obj.SetActive(true);
                        if (position.HasValue)
                            obj.transform.position = position.Value;
                        return obj;
                    }
                }
                Debug.LogWarning($"对象池 '{poolName}' 没有可用对象");
            }
            else
            {
                Debug.LogWarning($"对象池 '{poolName}' 不存在");
            }
            return null;
        }
        //将对象返回对象池并禁用
        public void ReturnToPool(string poolName, GameObject obj)
        {
            if (objectPools.TryGetValue(poolName, out var pool) && pool.Contains(obj))
            {
                obj.SetActive(false);
                obj.transform.SetParent(transform); // 重置父物体
                obj.transform.localPosition = Vector3.zero; // 重置位置
                Debug.Log($"将对象 '{obj.name}' 返回对象池 '{poolName}'");
            }
        }
        //添加多个对象到对象池
        public void AddMultipleToPool(string poolName, GameObject prefab, int count, string path = null)
        {
            if (!objectPools.ContainsKey(poolName))
            {
                objectPools[poolName] = new List<GameObject>();
                Debug.Log($"创建对象池 '{poolName}'");
            }
            for (int i = 0; i < count; i++)
            {
                GameObject obj;
                if (isTest)
                {
                    obj = Instantiate(prefab, transform.position, Quaternion.identity);
                }
                else
                {
                    obj = PhotonNetwork.Instantiate(path + prefab.name, transform.position, Quaternion.identity, 0);
                }

                if (obj == null)
                {
                    Debug.LogError($"Failed to instantiate object for pool '{poolName}' using prefab '{prefab?.name ?? "null"}'");
                    continue;
                }
                obj.transform.SetParent(transform);
                obj.SetActive(false);
                objectPools[poolName].Add(obj);
            }
        }
    }

    public class ObjectPoolConst
    {
        public const string ImpactCannonCommonPool = "ImpactCannonCommonPool";
        public const string ImpactCannonHitCommonPool = "ImpactCannonHitCommonPool";
        public const string TestPool = "TestPool";
        public const string ImpactCannonTriggerPool = "ImpactCannonTriggerPool";
    }
}
