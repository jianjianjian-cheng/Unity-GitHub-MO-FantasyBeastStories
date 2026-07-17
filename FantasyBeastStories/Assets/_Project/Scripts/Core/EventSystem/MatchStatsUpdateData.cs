namespace Core
{
    /// <summary>
    /// 对局统计数据更新数据，用于 MatchStatisticsManager → UI 层通信
    /// </summary>
    public class MatchStatsUpdateData : EventArgsBase
    {
        /// <summary>击杀总数</summary>
        public int TotalKills { get; set; }

        /// <summary>总伤害量</summary>
        public int TotalDamage { get; set; }

        /// <summary>所获总经验量</summary>
        public int TotalExperience { get; set; }

        /// <summary>本次获得的金币</summary>
        public int EarnedCoins { get; set; }

        /// <summary>对局时长（秒）</summary>
        public int MatchDurationSeconds { get; set; }

        /// <summary>是否为本局结算（最终数据）</summary>
        public bool IsFinal { get; set; }

        public MatchStatsUpdateData(
            int totalKills,
            int totalDamage,
            int totalExperience,
            int earnedCoins = 0,
            int matchDurationSeconds = 0,
            bool isFinal = false)
        {
            TotalKills = totalKills;
            TotalDamage = totalDamage;
            TotalExperience = totalExperience;
            EarnedCoins = earnedCoins;
            MatchDurationSeconds = matchDurationSeconds;
            IsFinal = isFinal;
        }
    }
}