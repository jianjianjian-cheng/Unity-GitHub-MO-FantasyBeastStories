using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manager
{
    public class ObjectPoolManager : ManagerBase
    {
        [SerializeField] private GameObject fireBallPrefab; // 火球预制体
        [SerializeField] private GameObject fireBallHitEffectPrefab; // 火球击中效果预制体
        void Start()
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
            if (objectPools.ContainsKey(poolName) && objectPools[poolName].Count > 0)
            {
                GameObject obj = objectPools[poolName][0];
                objectPools[poolName].RemoveAt(0);
                return obj;
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
                GameObject obj = Object.Instantiate(prefab);
                obj.SetActive(false);
                objectPools[poolName].Add(obj);
            }
        }
    }
}
