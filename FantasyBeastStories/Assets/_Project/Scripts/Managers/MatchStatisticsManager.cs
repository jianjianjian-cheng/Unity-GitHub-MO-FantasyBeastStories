using Core;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// 对局统计系统管理器（Application 层）
    ///
    /// 职责：
    /// - 对局中记录击杀数、总伤害、所获总经验量
    /// - 对局结束后（返回大厅时）结算统计结果
    /// - 结算时联动 CoinManager 计算并发放金币
    /// - 通过 EventChannel 通知 UI 层展示结算面板
    ///
    /// 设计说明：
    /// - 纯本地统计，每个玩家各自独立记录，无需网络同步
    /// - 对局中只记录原始数据（RecordKill / RecordDamage / RecordExperience）
    /// - FinalizeMatch() 在返回大厅时调用，生成最终结果
    /// - 从主菜单进入大厅时 HasPendingMatchResult 为 false，不展示结算
    /// </summary>
    public class MatchStatisticsManager : MonoBehaviour
    {
        public static MatchStatisticsManager Instance { get; private set; }

        // ──────────────────────────────────
        //  对局中原始数据（只增不减）
        // ──────────────────────────────────

        private int totalKillsInMatch;
        private int totalDamageInMatch;
        private int totalExpInMatch;
        private float matchStartTime;

        // ──────────────────────────────────
        //  生涯累计数据（跨对局持久化）
        // ──────────────────────────────────

        private int lifetimeKills;
        private int lifetimeDamage;
        private int lifetimeMatches;

        // ──────────────────────────────────
        //  待结算状态（用于判断是否展示结算面板）
        // ──────────────────────────────────

        /// <summary>是否有未结算的上一局数据</summary>
        public bool HasPendingMatchResult { get; private set; }

        /// <summary>待展示的结算数据</summary>
        public MatchStatsUpdateData PendingResult { get; private set; }

        // ──────────────────────────────────
        //  单例生命周期
        // ──────────────────────────────────

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ──────────────────────────────────
        //  对局中记录（由战斗系统调用）
        // ──────────────────────────────────

        /// <summary>记录一次击杀（击杀数 +1）</summary>
        public void RecordKill()
        {
            totalKillsInMatch++;
            QuestTaskManager.Instance?.RecordKill();
        }

        /// <summary>累加造成的伤害</summary>
        /// <param name="damage">本次伤害量（应 > 0）</param>
        public void RecordDamage(int damage)
        {
            if (damage <= 0) return;
            totalDamageInMatch += damage;
        }

        /// <summary>累加获得的经验量</summary>
        /// <param name="exp">本次获得的经验值（应 > 0）</param>
        public void RecordExperience(int exp)
        {
            if (exp <= 0) return;
            totalExpInMatch += exp;
        }

        // ──────────────────────────────────
        //  对局生命周期
        // ──────────────────────────────────

        /// <summary>
        /// 新对局开始时调用，将统计数据清零并记录开始时间
        /// </summary>
        public void ResetMatchStats()
        {
            totalKillsInMatch = 0;
            totalDamageInMatch = 0;
            totalExpInMatch = 0;
            matchStartTime = Time.time;
        }

        /// <summary>
        /// 对局结算：在返回大厅时调用。
        ///
        /// 执行流程：
        /// 1. 用当前记录生成 MatchStatsUpdateData
        /// 2. 调用 CoinManager.CalculateCoins() 计算应得金币
        /// 3. 调用 CoinManager.AddCoins() 实际发放金币
        /// 4. 设置 HasPendingMatchResult = true，保存结算数据
        /// 5. 通过 matchStatsUpdateChannel 发出结算事件（isFinal=true）
        /// 6. 重置对局统计数据，等待下一局
        /// </summary>
        /// <returns>本局结算数据</returns>
        public MatchStatsUpdateData FinalizeMatch()
        {
            // 判断是否有实际的对局数据（从主菜单 → 大厅时全为 0，不触发结算）
            bool hasActualData = totalKillsInMatch > 0 || totalDamageInMatch > 0 || totalExpInMatch > 0;

            // 计算应得金币（仅在有实际数据时发放）
            int earnedCoins = 0;
            if (CoinManager.Instance != null && hasActualData)
            {
                earnedCoins = CoinManager.Instance.CalculateCoins(totalKillsInMatch, totalDamageInMatch);
                CoinManager.Instance.AddCoins(earnedCoins);
                QuestTaskManager.Instance?.RecordCoin();
            }

            // 计算对局时长
            int matchDurationSeconds = matchStartTime > 0
                ? Mathf.RoundToInt(Time.time - matchStartTime)
                : 0;

            // 生成结算数据
            var result = new MatchStatsUpdateData(
                totalKills: totalKillsInMatch,
                totalDamage: totalDamageInMatch,
                totalExperience: totalExpInMatch,
                earnedCoins: earnedCoins,
                matchDurationSeconds: matchDurationSeconds,
                isFinal: hasActualData    // 无实际数据时 isFinal=false，面板不弹出
            );

            // 只有有实际数据时才标记待结算并通知 UI
            if (hasActualData)
            {
                HasPendingMatchResult = true;
                PendingResult = result;
                lifetimeKills += totalKillsInMatch;
                lifetimeDamage += totalDamageInMatch;
                lifetimeMatches++;
                QuestTaskManager.Instance?.RecordMatchComplete();
                RaiseMatchStatsUpdate(result);
            }

            // 重置统计数据，准备下一局
            ResetMatchStats();

            Debug.Log($"[MatchStatisticsManager] 对局结算完成：击杀={result.TotalKills}，" +
                      $"伤害={result.TotalDamage}，经验={result.TotalExperience}，获得金币={earnedCoins}");

            return result;
        }

        /// <summary>
        /// 结算面板已展示完毕，清除待结算标记。
        /// 由 UI 面板（MatchResultPanel）在关闭后调用。
        /// </summary>
        public void ConsumeMatchResult()
        {
            HasPendingMatchResult = false;
            PendingResult = null;
        }

        /// <summary>
        /// 放弃待结算数据（例如玩家直接返回主菜单）
        /// </summary>
        public void DiscardMatchResult()
        {
            HasPendingMatchResult = false;
            PendingResult = null;
        }

        // ──────────────────────────────────
        //  事件通信
        // ──────────────────────────────────

        /// <summary>
        /// 发送对局统计更新事件到 Presentation 层
        /// </summary>
        private void RaiseMatchStatsUpdate(MatchStatsUpdateData data)
        {
            if (EventChannelLocator.MainContainer?.matchStatsUpdateChannel == null) return;

            EventChannelLocator.MainContainer.matchStatsUpdateChannel.Raise(data);
        }


        #region  公共方法
        public int GetTotalKillsInMatch() => totalKillsInMatch;

        public void SetLifetimeStats(int kills, int damage, int matches)
        {
            lifetimeKills = kills;
            lifetimeDamage = damage;
            lifetimeMatches = matches;
        }

        /// <summary>
        /// 获取生涯累计统计（给 SaveManager 读档时调用）
        /// </summary>
        public (int kills, int damage, int matches) GetLifetimeStats()
        {
            return (lifetimeKills, lifetimeDamage, lifetimeMatches);
        }


        #endregion
    }
}