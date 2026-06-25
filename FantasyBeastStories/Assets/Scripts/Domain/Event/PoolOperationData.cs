using UnityEngine;

namespace Domain.Event
{
    public enum PoolOperationType
    {
        GetFromPoolAndActivate,
        ReturnToPool,
        AddMultipleToPool,
        Spawn,
        Despawn,
        DespawnAll,
        GetPoolCount
    }

    public class PoolOperationData : EventArgsBase
    {
        public PoolOperationType operationType;
        public string poolName;
        public GameObject targetObject;
        public Vector3 position;
        public Quaternion rotation;
        public float delay;
        public int count;
        public GameObject prefab;
        public System.Action<GameObject> resultCallback;
        public System.Action<int> countCallback;

        public static PoolOperationData CreateGet(string poolName, Vector3 position, System.Action<GameObject> callback)
        {
            return new PoolOperationData
            {
                operationType = PoolOperationType.GetFromPoolAndActivate,
                poolName = poolName,
                position = position,
                resultCallback = callback
            };
        }

        public static PoolOperationData CreateReturn(string poolName, GameObject obj)
        {
            return new PoolOperationData
            {
                operationType = PoolOperationType.ReturnToPool,
                poolName = poolName,
                targetObject = obj
            };
        }

        public static PoolOperationData CreateAddMultiple(string poolName, GameObject prefab, int count)
        {
            return new PoolOperationData
            {
                operationType = PoolOperationType.AddMultipleToPool,
                poolName = poolName,
                prefab = prefab,
                count = count
            };
        }

        public static PoolOperationData CreateSpawn(string poolName, Vector3 position, Quaternion rotation, System.Action<GameObject> callback)
        {
            return new PoolOperationData
            {
                operationType = PoolOperationType.Spawn,
                poolName = poolName,
                position = position,
                rotation = rotation,
                resultCallback = callback
            };
        }

        public static PoolOperationData CreateDespawn(string poolName, GameObject obj, float delay = 0f)
        {
            return new PoolOperationData
            {
                operationType = PoolOperationType.Despawn,
                poolName = poolName,
                targetObject = obj,
                delay = delay
            };
        }

        public static PoolOperationData CreateGetPoolCount(string poolName, System.Action<int> callback)
        {
            return new PoolOperationData
            {
                operationType = PoolOperationType.GetPoolCount,
                poolName = poolName,
                countCallback = callback
            };
        }
    }
}