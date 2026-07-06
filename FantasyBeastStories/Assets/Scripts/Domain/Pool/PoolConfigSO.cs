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
            // 本地经验球池（非网络对象）
            new PoolInitializationEntry { poolName = PoolConst.ExperienceBall_Blue_Local, preloadCount = 50 },
            //BingNv 角色专属对象池
            new PoolInitializationEntry { poolName = PoolConst.GuiLingFirePool, preloadCount = 10 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingLightningPool, preloadCount = 10 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingWinterPool, preloadCount = 10 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingGrassPool, preloadCount = 10 },
            //GuiLing 击中特效池
            new PoolInitializationEntry { poolName = PoolConst.GuiLingHitFirePool, preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingHitLightningPool, preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingHitWinterPool, preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingHitGrassPool, preloadCount = 20 },
        };
    }
}