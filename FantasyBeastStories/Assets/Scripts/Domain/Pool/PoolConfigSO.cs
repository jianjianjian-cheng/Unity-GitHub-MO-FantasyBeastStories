using UnityEngine;

namespace Domain.Pool
{
    [System.Serializable]
    public class PoolInitializationEntry
    {
        public string poolName;
        public GameObject prefab;
        public int preloadCount = 10;
    }

    [CreateAssetMenu(menuName = "Config/Pool Config")]
    public class PoolConfigSO : ScriptableObject
    {
        public PoolInitializationEntry[] pools = new PoolInitializationEntry[]
        {
            new PoolInitializationEntry { poolName = PoolConst.TestPool, preloadCount = 0 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonCommonPool, preloadCount = 10 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonHitCommonPool, preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonLightenPool, preloadCount = 10 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonHitLightenPool, preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonWinterPool, preloadCount = 10 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonHitWinterPool, preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonGrassPool, preloadCount = 10 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonHitGrassPool, preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonTriggerPool, preloadCount = 10 },
            new PoolInitializationEntry { poolName = PoolConst.FireFirePool, preloadCount = 10 },
            new PoolInitializationEntry { poolName = PoolConst.DamageNumPool, preloadCount = 100 },
        };
    }
}