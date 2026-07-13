namespace Domain.Pool
{
    public static class PoolConst
    {
        // 网络对象池常量（从 Infrastructure.Network.NetworkObjectPoolConst 合并）
        public const string Skeleton = "SkeletonPool";
        public const string ExperienceBall_Blue = "ExperienceBall_BluePool";

        // 本地对象池常量（经验球改为非网络对象后的本地池）
        public const string ExperienceBall_Blue_Local = "ExperienceBall_Blue_LocalPool";

        // 对象池常量（从 Domain.Pool.ObjectPoolConst 合并）
        public const string DamageNumPool = "DamageNumPool";
        public const string ImpactCannonCommonPool = "ImpactCannonCommonPool";
        public const string ImpactCannonLightenPool = "ImpactCannonLightenPool";
        public const string ImpactCannonHitCommonPool = "ImpactCannonHitCommonPool";
        public const string ImpactCannonHitLightenPool = "ImpactCannonHitLightenPool";
        public const string ImpactCannonWinterPool = "ImpactCannonWinterPool";
        public const string ImpactCannonHitWinterPool = "ImpactCannonHitWinterPool";
        public const string ImpactCannonGrassPool = "ImpactCannonGrassPool";
        public const string ImpactCannonHitGrassPool = "ImpactCannonHitGrassPool";
        public const string TestPool = "TestPool";
        public const string ImpactCannonTriggerPool = "ImpactCannonTriggerPool";
        public const string FireFirePool = "FireFirePool";


        //BingNv 角色专属对象池常量
        // ====== GuiLing（投射物） ======
        public const string GuiLingFirePool = "GuiLingFirePool";
        public const string GuiLingLightningPool = "GuiLingLightningPool";
        public const string GuiLingWinterPool = "GuiLingWinterPool";
        public const string GuiLingGrassPool = "GuiLingGrassPool";

        // ====== GuiLingHit（击中特效） ======
        public const string GuiLingHitFirePool = "GuiLingHitFirePool";
        public const string GuiLingHitLightningPool = "GuiLingHitLightningPool";
        public const string GuiLingHitWinterPool = "GuiLingHitWinterPool";
        public const string GuiLingHitGrassPool = "GuiLingHitGrassPool";

        // ====== PowerUp（道具系统） ======
        public const string PowerUpItem = "PowerUpItemPool";
    }
}