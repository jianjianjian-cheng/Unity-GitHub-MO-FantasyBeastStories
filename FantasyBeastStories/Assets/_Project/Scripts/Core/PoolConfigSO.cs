using UnityEngine;

namespace Core
{
    [System.Serializable]
    public class PoolInitializationEntry
    {
        public string poolName;
        public GameObject prefab;          // 兼容旧数据，运行时优先使用 addressableKey
        [Tooltip("Addressables 地址 key，运行时通过此 key 加载预制体（热更生效）")]
        public string addressableKey;
        public int preloadCount = 10;
    }

    [CreateAssetMenu(menuName = "Config/Pool Config")]
    public class PoolConfigSO : ScriptableObject
    {
        public PoolInitializationEntry[] pools = new PoolInitializationEntry[]
        {
            new PoolInitializationEntry { poolName = PoolConst.TestPool, addressableKey = "ImpactCannon/ImpactCannonWinter", preloadCount = 0 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonCommonPool, addressableKey = "ImpactCannon/ImpactCannonCommon", preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonHitCommonPool, addressableKey = "ImpactCannon/ImpactCannonHitCommon", preloadCount = 40 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonLightenPool, addressableKey = "ImpactCannon/ImpactCannonLighten", preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonHitLightenPool, addressableKey = "ImpactCannon/ImpactCannonHitLighten", preloadCount = 40 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonWinterPool, addressableKey = "ImpactCannon/ImpactCannonWinter", preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonHitWinterPool, addressableKey = "ImpactCannon/ImpactCannonHitWinter", preloadCount = 40 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonGrassPool, addressableKey = "ImpactCannon/ImpactCannonGrass", preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonHitGrassPool, addressableKey = "ImpactCannon/ImpactCannonHitGrass", preloadCount = 40 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonTriggerPool, addressableKey = "ImpactCannon/ImpactCannonTrigger", preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.FireFirePool, addressableKey = "ImpactCannon/ImpactCannonLighten", preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.DamageNumPool, addressableKey = "PoolPrefabs/DamageNum", preloadCount = 100 },
            new PoolInitializationEntry { poolName = PoolConst.ExperienceBall_Blue_Local, addressableKey = "PoolPrefabs/ExperienceBall_Blue", preloadCount = 50 },
            new PoolInitializationEntry { poolName = PoolConst.PowerUpItem, addressableKey = "PoolPrefabs/PowerUpItem", preloadCount = 5 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingFirePool, addressableKey = "GuiLing/GuiLingFire", preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingLightningPool, addressableKey = "GuiLing/GuiLingLightning", preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingWinterPool, addressableKey = "GuiLing/GuiLingWinter", preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingGrassPool, addressableKey = "GuiLing/GuiLingGrass", preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingHitFirePool, addressableKey = "GuiLing/GuiLingHitFire", preloadCount = 40 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingHitLightningPool, addressableKey = "GuiLing/GuiLingHitLightning", preloadCount = 40 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingHitWinterPool, addressableKey = "GuiLing/GuiLingHitWinter", preloadCount = 40 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingHitGrassPool, addressableKey = "GuiLing/GuiLingHitGrass", preloadCount = 40 },
        };
    }
}