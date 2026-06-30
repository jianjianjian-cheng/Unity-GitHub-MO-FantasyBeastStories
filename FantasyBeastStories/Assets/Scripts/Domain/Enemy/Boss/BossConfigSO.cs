using UnityEngine;

namespace Domain.Enemy.Boss
{
    /// <summary>
    /// Boss配置数据（ScriptableObject）
    /// 将Boss的所有可配参数从MonoBehaviour中分离，实现数据驱动
    /// </summary>
    [CreateAssetMenu(menuName = "Config/Boss Config")]
    public class BossConfigSO : ScriptableObject
    {
        [Header("Boss基本信息")]
        public string bossName = "蛛王菲力甫斯";
        public float maxHealth = 100000f;
        public float attackPower = 30f;

        [Header("咬击 (Bite)")]
        public float biteRange = 2f;
        public float biteDamage = 3f;
        public float biteWindUp = 0.4f;
        public float biteCooldown = 2f;

        [Header("射线 (RayBeam)")]
        public float rayBeamSpeed = 8f;
        public float rayBeamDamageMultiplier = 5f;
        public float rayBeamWindUp = 0.5f;
        public float rayBeamCooldown = 12f;

        [Header("连续火球 (FireballBurst)")]
        public float fireballSpeed = 2f;
        public int fireballBurstCount = 5;
        public float fireballBurstInterval = 0.2f;
        public float fireballBurstSpread = 15f;
        public float fireballBurstWindUp = 0.7f;
        public float fireballBurstCooldown = 8f;
        public float fireballDamageMultiplier = 6f;

        [Header("滚动追踪 (Roll)")]
        public float rollSpeed = 12f;
        public float rollDuration = 2f;
        public float rollDamage = 5f;
        public float rollWindUp = 0.6f;
        public float rollCooldown = 10f;
        public float rollTurnSpeed = 120f;

        [Header("阶段参数")]
        [Tooltip("进入二阶段的血量百分比阈值")]
        public float phase2HealthPercent = 0.5f;

        [Header("一阶段")]
        public float phase1MoveSpeed = 2f;
        public float phase1PreferredDistance = 5f;

        [Header("二阶段")]
        public float phase2MoveSpeed = 1f;
        public float phase2PreferredDistance = 4f;
        [Tooltip("二阶段冷却时间倍率（<1 表示技能冷却更快）")]
        public float phase2CooldownMultiplier = 0.7f;

        [Header("防卡死参数")]
        public float maxAdjustTime = 5f;
        public float idleActionInterval = 1f;
    }
}