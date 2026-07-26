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
            new PoolInitializationEntry { poolName = PoolConst.TestPool, addressableKey = "Level1_ImpactCannon_ImpactCannonWinter", preloadCount = 0 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonCommonPool, addressableKey = "Level1_ImpactCannon_ImpactCannonCommon", preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonHitCommonPool, addressableKey = "Level1_ImpactCannon_ImpactCannonHitCommon", preloadCount = 40 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonLightenPool, addressableKey = "Level1_ImpactCannon_ImpactCannonLighten", preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonHitLightenPool, addressableKey = "Level1_ImpactCannon_ImpactCannonHitLighten", preloadCount = 40 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonWinterPool, addressableKey = "Level1_ImpactCannon_ImpactCannonWinter", preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonHitWinterPool, addressableKey = "Level1_ImpactCannon_ImpactCannonHitWinter", preloadCount = 40 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonGrassPool, addressableKey = "Level1_ImpactCannon_ImpactCannonGrass", preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonHitGrassPool, addressableKey = "Level1_ImpactCannon_ImpactCannonHitGrass", preloadCount = 40 },
            new PoolInitializationEntry { poolName = PoolConst.ImpactCannonTriggerPool, addressableKey = "Level1_ImpactCannon_ImpactCannonTrigger", preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.FireFirePool, addressableKey = "Level1_ImpactCannon_ImpactCannonLighten", preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.DamageNumPool, addressableKey = "Level1_PoolPrefabs_DamageNum", preloadCount = 100 },
            new PoolInitializationEntry { poolName = PoolConst.ExperienceBall_Blue_Local, addressableKey = "Level1_PoolPrefabs_ExperienceBall_Blue", preloadCount = 50 },
            new PoolInitializationEntry { poolName = PoolConst.PowerUpItem, addressableKey = "Level1_PoolPrefabs_PowerUpItem", preloadCount = 5 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingFirePool, addressableKey = "Level1_GuiLing_GuiLingFire", preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingLightningPool, addressableKey = "Level1_GuiLing_GuiLingLightning", preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingWinterPool, addressableKey = "Level1_GuiLing_GuiLingWinter", preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingGrassPool, addressableKey = "Level1_GuiLing_GuiLingGrass", preloadCount = 20 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingHitFirePool, addressableKey = "Level1_GuiLing_GuiLingHitFire", preloadCount = 40 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingHitLightningPool, addressableKey = "Level1_GuiLing_GuiLingHitLightning", preloadCount = 40 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingHitWinterPool, addressableKey = "Level1_GuiLing_GuiLingHitWinter", preloadCount = 40 },
            new PoolInitializationEntry { poolName = PoolConst.GuiLingHitGrassPool, addressableKey = "Level1_GuiLing_GuiLingHitGrass", preloadCount = 40 },
        };
    }
}