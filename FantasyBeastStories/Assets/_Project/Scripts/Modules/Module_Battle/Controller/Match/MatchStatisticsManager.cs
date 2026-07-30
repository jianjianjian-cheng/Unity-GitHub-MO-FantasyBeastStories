using Core;
using Core.SharedModel;
using Core.Save;
using UnityEngine;
using Controllers.Battle;
using Controllers.Game;
using Controllers.Task;

namespace Controllers.Battle
{
    /// <summary>
    /// 对局统计控制器 — 薄层 MonoBehaviour，持有 MatchStatisticsModel 实例。
    ///
    /// 职责：
    /// - 生命周期管理（单例 + DontDestroyOnLoad）
    /// - 存档注册（ISaveable）
    /// - 处理外部依赖（CoinManager / QuestTaskManager / Time.time）
    /// - 业务逻辑委托给 MatchStatisticsModel
    /// </summary>
    public class MatchStatisticsManager : MonoBehaviour, ISaveable
    {
        private static MatchStatisticsManager _instance;

        /// <summary>统计模型实例（纯 C#，可单测）</summary>
        public MatchStatisticsModel Model { get; private set; }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[MatchStatisticsManager.Awake] 重复实例销毁: new={GetHashCode()}, existing={_instance.GetHashCode()}");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            ServiceLocator.Register(this);
            DontDestroyOnLoad(gameObject);
            Model = new MatchStatisticsModel();
            Debug.Log($"[MatchStatisticsManager.Awake] instance={GetHashCode()}, scene={gameObject.scene.name}");
        }

        void Start()
        {
            ServiceLocator.Get<SaveManager>()?.RegisterSaveable(this);
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                ServiceLocator.Unregister<MatchStatisticsManager>();
            }
            ServiceLocator.Get<SaveManager>()?.UnregisterSaveable(this);
        }

        // ========== ISaveable 实现 ==========

        public string SaveId => "MatchStatisticsManager";

        public void OnSave(SaveData data)
        {
            var (kills, damage, matches) = Model.GetLifetimeStats();
            data.lifetimeKills = kills;
            data.lifetimeDamage = damage;
            data.lifetimeMatches = matches;
        }

        public void OnLoad(SaveData data)
        {
            Model.SetLifetimeStats(data.lifetimeKills, data.lifetimeDamage, data.lifetimeMatches);
        }

        // ========== 对局中记录（含外部联动） ==========

        /// <summary>记录一次击杀，联动 QuestTaskManager</summary>
        public void RecordKill()
        {
            Model.RecordKill();
            Debug.Log($"[MatchStatisticsManager.RecordKill] kills={Model.TotalKillsInMatch} (instance={GetHashCode()})");
            ServiceLocator.Get<QuestTaskManager>()?.RecordKill();
        }

        public void RecordDamage(int damage)
        {
            Model.RecordDamage(damage);
            Debug.Log($"[MatchStatisticsManager.RecordDamage] damage={Model.TotalDamageInMatch} (instance={GetHashCode()})");
        }

        public void RecordExperience(int exp)
        {
            Model.RecordExperience(exp);
            Debug.Log($"[MatchStatisticsManager.RecordExperience] exp={Model.TotalExpInMatch} (instance={GetHashCode()})");
        }

        // ========== 对局生命周期 ==========

        public void ResetMatchStats()
        {
            Debug.Log($"[MatchStatisticsManager.ResetMatchStats] BEFORE: kills={Model.TotalKillsInMatch}, damage={Model.TotalDamageInMatch}, exp={Model.TotalExpInMatch} (instance={GetHashCode()})");
            Model.ResetMatchStats(UnityEngine.Time.time);
        }

        /// <summary>
        /// 对局结算：计算金币 → Model 结算 → 发放金币 → 联动任务系统
        /// </summary>
        public MatchStatsUpdateData FinalizeMatch()
        {
            // 1. 通过 CoinManager 计算应得金币
            int earnedCoins = 0;
            var (kills, damage, _) = Model.GetLifetimeStats(); // not used for calc
            int matchKills = GetTotalKillsInMatch();
            int matchDamage = GetMatchDamageInMatch();

            bool hasActualData = matchKills > 0 || matchDamage > 0 || Model.TotalExpInMatch > 0;

            if (ServiceLocator.Get<CoinManager>() != null && hasActualData)
            {
                earnedCoins = ServiceLocator.Get<CoinManager>().CalculateCoins(matchKills, matchDamage);
            }

            // 2. Model 完成结算（更新生涯累计、生成结果、通知 UI）
            var result = Model.FinalizeMatch(UnityEngine.Time.time, earnedCoins);

            // 3. 发放金币 + 联动任务系统
            if (hasActualData)
            {
                if (earnedCoins > 0)
                {
                    ServiceLocator.Get<CoinManager>()?.AddCoins(earnedCoins);
                    ServiceLocator.Get<QuestTaskManager>()?.RecordCoin();
                }
                ServiceLocator.Get<QuestTaskManager>()?.RecordMatchComplete();
            }

            Debug.Log($"[MatchStatisticsManager] 对局结算完成：击杀={result.TotalKills}，" +
                      $"伤害={result.TotalDamage}，经验={result.TotalExperience}，获得金币={earnedCoins}");

            return result;
        }

        public void ConsumeMatchResult() => Model.ConsumeMatchResult();
        public void DiscardMatchResult() => Model.DiscardMatchResult();

        // ========== 便捷转发（向后兼容） ==========

        public bool HasPendingMatchResult => Model.HasPendingMatchResult;
        public MatchStatsUpdateData PendingResult => Model.PendingResult;
        public int GetTotalKillsInMatch() => Model.TotalKillsInMatch;
        private int GetMatchDamageInMatch() => Model.TotalDamageInMatch;

        public void SetLifetimeStats(int kills, int damage, int matches)
            => Model.SetLifetimeStats(kills, damage, matches);

        public (int kills, int damage, int matches) GetLifetimeStats()
            => Model.GetLifetimeStats();
    }
}