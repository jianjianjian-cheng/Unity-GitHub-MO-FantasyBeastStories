using UnityEngine;

namespace Controllers.Enemy
{
    /// <summary>
    /// 怪物数值与数量随时间变化的计算器
    ///
    /// 时间线（总 15 分钟 = 900 秒）：
    ///   0s ─────────────── 600s(10min) ─────────────── 900s(15min,Boss出现)
    ///   │   数量逐渐增加     │   数量达到峰值(2x)       │   Boss出现后数量骤降(限制)
    ///   │   血量逐渐增加     │   血量持续上升           │   血量回落至初始值
    ///
    /// 数量曲线：0.5x(前3min) → 1x~2x(3~10min) → 维持2x → 骤降至0.5x(Boss出现后限流)
    /// 血量曲线：1x → 4x（900s 前线性增长）→ Boss 出现后回落至 1x
    /// </summary>
    public static class EnemyScalingCalculator
    {
        // ── 时间常量 ──
        private const float BossSpawnTime = 900f;   // 15 分钟 Boss 出现
        private const float PeakCountTime = 600f;   // 10 分钟数量达到峰值
        private const float EarlyGameEndTime = 180f; // 3 分钟前期结束

        // ── 数量倍率 ──
        private const float EarlyGameCountMultiplier = 0.5f;  // 前 3 分钟数量减半
        private const float MidGameCountMultiplier = 1f;      // 3 分钟后恢复正常数量
        private const float PeakCountMultiplier = 2f;          // 峰值数量倍率（初始的 2 倍）
        private const float PostBossCountMultiplier = 1f;     // Boss 出现后数量倍率

        // ── 血量倍率 ──
        private const float InitialHpMultiplier = 1f;   // 初始血量倍率
        private const float PeakHpMultiplier = 4f;      // 峰值血量倍率（初始的 4 倍）
        private const float PostBossHpMultiplier = 1f; // Boss 出现后血量回落至初始

        // ── 过渡时间 ──
        private const float BossTransitionDuration = 60f; // Boss 出现后 60 秒内平滑过渡到初始值

        /// <summary>
        /// 根据当前游戏时间计算数量倍率
        /// 0~3min: 0.5x（前期减半）
        /// 3~10min: 1x → 2x 线性增长
        /// 10~15min: 维持 2x
        /// 15min+: 从 2x 平滑降至 0.5x（60秒过渡）
        /// </summary>
        public static float GetCountMultiplier(float currentTime)
        {
            if (currentTime < EarlyGameEndTime)
            {
                // 前 3 分钟：数量减半
                return EarlyGameCountMultiplier;
            }

            if (currentTime < PeakCountTime)
            {
                // 3 → 10min: 1x → 2x 线性增长
                float progress = (currentTime - EarlyGameEndTime) / (PeakCountTime - EarlyGameEndTime);
                return Mathf.Lerp(MidGameCountMultiplier, PeakCountMultiplier, progress);
            }

            if (currentTime < BossSpawnTime)
            {
                // 10 → 15min: 维持峰值
                return PeakCountMultiplier;
            }

            // Boss 出现后：从 2x → 0.5x 平滑过渡（60秒）
            if (currentTime < BossSpawnTime + BossTransitionDuration)
            {
                float progress = (currentTime - BossSpawnTime) / BossTransitionDuration;
                return Mathf.Lerp(PeakCountMultiplier, PostBossCountMultiplier, progress);
            }

            return PostBossCountMultiplier;
        }

        /// <summary>
        /// 根据当前游戏时间计算血量倍率
        /// 0~15min: 1x → 4x 线性增长
        /// 15min+: 从 4x → 1x 平滑过渡（60秒）
        /// </summary>
        public static float GetHpMultiplier(float currentTime)
        {
            if (currentTime < BossSpawnTime)
            {
                // 0 → 15min: 1x → 4x 线性增长
                float progress = currentTime / BossSpawnTime;
                return Mathf.Lerp(InitialHpMultiplier, PeakHpMultiplier, progress);
            }

            // Boss 出现后：从 4x → 1x 平滑过渡（60秒）
            if (currentTime < BossSpawnTime + BossTransitionDuration)
            {
                float progress = (currentTime - BossSpawnTime) / BossTransitionDuration;
                return Mathf.Lerp(PeakHpMultiplier, PostBossHpMultiplier, progress);
            }

            return PostBossHpMultiplier;
        }

        /// <summary>
        /// 根据当前游戏时间计算生成间隔倍率
        /// 数量倍率越高 → 生成间隔越短（更快生成）
        /// interval = baseInterval / countMultiplier
        /// </summary>
        public static float GetSpawnIntervalMultiplier(float currentTime)
        {
            return 1f / GetCountMultiplier(currentTime);
        }

        /// <summary>Boss 是否已出现</summary>
        public static bool IsBossPhase(float currentTime) => currentTime >= BossSpawnTime;

        // ── 玩家数量倍率（1 人为基准 1x）──

        /// <summary>
        /// 根据玩家数量获取怪物数量倍率（影响生成频率）
        /// 1人: 1x | 2人: 1.5x | 3人: 2x | 4人: 2.5x
        /// </summary>
        public static float GetPlayerCountMultiplier(int playerCount)
        {
            switch (playerCount)
            {
                case 1: return 1f;
                case 2: return 1.5f;
                case 3: return 2f;
                default: return playerCount >= 4 ? 2.5f : 1f;
            }
        }

        /// <summary>
        /// 根据玩家数量获取怪物血量倍率
        /// 1人: 1x | 2人: 2x | 3人: 2.5x | 4人: 3x
        /// </summary>
        public static float GetPlayerHpMultiplier(int playerCount)
        {
            switch (playerCount)
            {
                case 1: return 1f;
                case 2: return 2f;
                case 3: return 2.5f;
                default: return playerCount >= 4 ? 3f : 1f;
            }
        }

        /// <summary>
        /// 根据玩家数量获取怪物最大数量倍率（影响上限）
        /// 1人: 1x | 2人: 1.5x | 3人: 2x | 4人: 2.5x
        /// </summary>
        public static float GetPlayerMaxCountMultiplier(int playerCount)
        {
            switch (playerCount)
            {
                case 1: return 1f;
                case 2: return 1.5f;
                case 3: return 2f;
                default: return playerCount >= 4 ? 2.5f : 1f;
            }
        }

        // ── Dragon 生成概率 ──
        private const float DragonPeakTime = 480f;       // 8 分钟达到峰值
        private const float DragonInitialProbability = 0.05f;  // 初始 5%
        private const float DragonPeakProbability = 0.30f;     // 峰值 30%

        /// <summary>
        /// 根据当前游戏时间计算 Dragon 生成概率
        /// 0s: 5%（初始）
        /// 0~480s(8min): 5% → 30% 线性增长
        /// 480s+: 维持 30%
        /// </summary>
        public static float GetDragonSpawnProbability(float currentTime)
        {
            if (currentTime >= DragonPeakTime)
                return DragonPeakProbability;

            float progress = currentTime / DragonPeakTime;
            return Mathf.Lerp(DragonInitialProbability, DragonPeakProbability, progress);
        }
    }
}
