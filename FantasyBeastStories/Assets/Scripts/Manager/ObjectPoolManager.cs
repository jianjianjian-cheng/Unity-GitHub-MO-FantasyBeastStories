using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace Manager
{
    public class ObjectPoolManager : ManagerBase
    {
        [SerializeField] private GameObject fireBallPrefab; // 火球预制体
        [SerializeField] private GameObject fireBallHitEffectPrefab; // 火球击中效果预制体
        private bool isPhotonReady = false; // Photon是否准备就绪
        void Start()
        {
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
            if (!isPhotonReady && PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
            {
                InitializePool();
                isPhotonReady = true;
            }
        }

        private void InitializePool()
        {
            //添加火球到对象池
            AddMultipleToPool("FireBallPool", fireBallPrefab, 10);
            //添加火球击中效果到对象池
            AddMultipleToPool("FireBallHitEffectPool", fireBallHitEffectPrefab, 10);
        }
        //对象池字典
        private Dictionary<string, List<GameObject>> objectPools = new Dictionary<string, List<GameObject>>();
        //添加对象到对象池
        public void AddToPool(string poolName, GameObject obj)
        {
            if (!objectPools.ContainsKey(poolName))
            {
                objectPools[poolName] = new List<GameObject>();
            }
            objectPools[poolName].Add(obj);
        }
        //从对象池获取对象
        public GameObject GetFromPool(string poolName)
        {
            if (objectPools.ContainsKey(poolName))
            {
                // Iterate and return first inactive (available) object.
                // Clean up any destroyed objects that remained in the pool.
                for (int i = 0; i < objectPools[poolName].Count; i++)
                {
                    GameObject obj = objectPools[poolName][i];
                    if (obj == null)
                    {
                        // Removed object was destroyed elsewhere but still in the pool.
                        objectPools[poolName].RemoveAt(i);
                        i--;
                        continue;
                    }

                    if (!obj.activeSelf)
                    {
                        objectPools[poolName].RemoveAt(i);
                        return obj;
                    }
                }
            }
            return null;
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
            GameObject obj = GetFromPool(poolName);
            if (obj != null)
            {
                obj.SetActive(true);
                if (position.HasValue)
                {
                    obj.transform.position = position.Value;
                }
            }
            return obj;
        }
        //将对象返回对象池并禁用
        public void ReturnToPool(string poolName, GameObject obj)
        {
            if (objectPools.ContainsKey(poolName))
            {
                obj.SetActive(false);
                objectPools[poolName].Add(obj);
            }
        }
        //添加多个对象到对象池
        public void AddMultipleToPool(string poolName, GameObject prefab, int count)
        {
            if (!objectPools.ContainsKey(poolName))
            {
                objectPools[poolName] = new List<GameObject>();
            }
            for (int i = 0; i < count; i++)
            {
                GameObject obj = PhotonNetwork.Instantiate(prefab.name, transform.position, Quaternion.identity);
                obj.SetActive(false);
                objectPools[poolName].Add(obj);
            }
        }
    }
}
