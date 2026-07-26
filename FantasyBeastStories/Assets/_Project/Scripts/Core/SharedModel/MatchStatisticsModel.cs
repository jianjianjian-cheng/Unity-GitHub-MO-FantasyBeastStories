using UnityEngine;

namespace Core.SharedModel
{
    /// <summary>
    /// 对局统计模型 — 纯 C# 类，不继承 MonoBehaviour，可独立单元测试。
    ///
    /// 持有两类数据：
    /// - 对局中临时数据（击杀/伤害/经验/开始时间）
    /// - 生涯累计数据（跨对局持久化）
    ///
    /// 外部依赖（CoinManager/QuestTaskManager/Time.time）由 Controller 传入，
    /// Model 自身只处理数据变更与 EventChannel 通知。
    /// </summary>
    public class MatchStatisticsModel
    {
        // ──────────────────────────────────
        //  对局中临时数据
        // ──────────────────────────────────

        public int TotalKillsInMatch { get; private set; }
        public int TotalDamageInMatch { get; private set; }
        public int TotalExpInMatch { get; private set; }
        private float _matchStartTime;

        // ──────────────────────────────────
        //  生涯累计数据
        // ──────────────────────────────────

        public int LifetimeKills { get; private set; }
        public int LifetimeDamage { get; private set; }
        public int LifetimeMatches { get; private set; }

        // ──────────────────────────────────
        //  待结算状态
        // ──────────────────────────────────

        public bool HasPendingMatchResult { get; private set; }
        public MatchStatsUpdateData PendingResult { get; private set; }

        // ──────────────────────────────────
        //  对局中记录
        // ──────────────────────────────────

        public void RecordKill()
        {
            TotalKillsInMatch++;
        }

        public void RecordDamage(int damage)
        {
            if (damage <= 0) return;
            TotalDamageInMatch += damage;
        }

        public void RecordExperience(int exp)
        {
            if (exp <= 0) return;
            TotalExpInMatch += exp;
        }

        // ──────────────────────────────────
        //  对局生命周期
        // ──────────────────────────────────

        /// <summary>新对局开始时清零并记录开始时间</summary>
        /// <param name="startTime">由 Controller 传入 Time.time</param>
        public void ResetMatchStats(float startTime)
        {
            TotalKillsInMatch = 0;
            TotalDamageInMatch = 0;
            TotalExpInMatch = 0;
            _matchStartTime = startTime;
        }

        /// <summary>
        /// 对局结算：生成结算数据、更新生涯累计、通知 UI。
        ///
        /// earnedCoins 由 Controller 通过 CoinManager.CalculateCoins() 计算后传入，
        /// Model 不直接依赖 CoinManager。
        /// </summary>
        /// <param name="currentTime">由 Controller 传入 Time.time</param>
        /// <param name="earnedCoins">由 Controller 计算后传入</param>
        /// <returns>本局结算数据</returns>
        public MatchStatsUpdateData FinalizeMatch(float currentTime, int earnedCoins)
        {
            bool hasActualData = TotalKillsInMatch > 0 || TotalDamageInMatch > 0 || TotalExpInMatch > 0;

            int matchDurationSeconds = _matchStartTime > 0
                ? Mathf.RoundToInt(currentTime - _matchStartTime)
                : 0;

            var result = new MatchStatsUpdateData(
                totalKills: TotalKillsInMatch,
                totalDamage: TotalDamageInMatch,
                totalExperience: TotalExpInMatch,
                earnedCoins: earnedCoins,
                matchDurationSeconds: matchDurationSeconds,
                isFinal: hasActualData
            );

            if (hasActualData)
            {
                HasPendingMatchResult = true;
                PendingResult = result;

                LifetimeKills += TotalKillsInMatch;
                LifetimeDamage += TotalDamageInMatch;
                LifetimeMatches++;

                RaiseMatchStatsUpdate(result);
            }

            // 重置对局数据，准备下一局
            ResetMatchStats(currentTime);

            return result;
        }

        public void ConsumeMatchResult()
        {
            HasPendingMatchResult = false;
            PendingResult = null;
        }

        public void DiscardMatchResult()
        {
            HasPendingMatchResult = false;
            PendingResult = null;
        }

        // ──────────────────────────────────
        //  生涯数据
        // ──────────────────────────────────

        public void SetLifetimeStats(int kills, int damage, int matches)
        {
            LifetimeKills = kills;
            LifetimeDamage = damage;
            LifetimeMatches = matches;
        }

        public (int kills, int damage, int matches) GetLifetimeStats()
        {
            return (LifetimeKills, LifetimeDamage, LifetimeMatches);
        }

        // ──────────────────────────────────
        //  事件通信
        // ──────────────────────────────────

        private void RaiseMatchStatsUpdate(MatchStatsUpdateData data)
        {
            if (EventChannelLocator.MainContainer?.matchStatsUpdateChannel == null) return;
            EventChannelLocator.MainContainer.matchStatsUpdateChannel.Raise(data);
        }
    }
}
