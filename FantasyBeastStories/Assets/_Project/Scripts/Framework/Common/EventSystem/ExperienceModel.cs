using System.Collections.Generic;

namespace Core.SharedModel
{
    /// <summary>
    /// 经验/等级模型 — 纯 C# 类，不继承 MonoBehaviour，可独立单元测试。
    ///
    /// 持有：
    /// - 经验值 / 等级 / 升级阈值
    /// - 升级队列（pendingLevelUps）
    /// - 经验球 ID 去重（nextBallId / claimedBalls）
    /// - 专属卡牌奖励等级标记（lastBonusCheckLevel）
    ///
    /// 外部依赖（RPC / ObjectPool / MatchStatisticsManager / QuestTaskManager / Coroutine）
    /// 全部由 Controller 处理，Model 只管理数据与 EventChannel 通知。
    /// </summary>
    public class ExperienceModel
    {
        // ──────────────────────────────────
        //  经验 / 等级状态
        // ──────────────────────────────────

        public int CurrentExperience { get; private set; }
        public int CurrentLevel { get; private set; }
        public int UpgradeExperience { get; private set; }

        // ──────────────────────────────────
        //  升级队列
        // ──────────────────────────────────

        private readonly Queue<int> _pendingLevelUps = new();
        public bool IsProcessingLevelUp { get; set; }
        public int PendingLevelUpCount => _pendingLevelUps.Count;
        public bool HasPendingLevelUps => _pendingLevelUps.Count > 0;

        // ──────────────────────────────────
        //  经验球 ID 去重
        // ──────────────────────────────────

        private uint _nextBallId = 1;
        private readonly HashSet<uint> _claimedBalls = new();

        // ──────────────────────────────────
        //  专属卡牌奖励
        // ──────────────────────────────────

        private int _lastBonusCheckLevel = -1;

        // ──────────────────────────────────
        //  初始化
        // ──────────────────────────────────

        /// <summary>新对局初始化</summary>
        public void Initialize()
        {
            UpgradeExperience = 150;
            _nextBallId = 1;
            _claimedBalls.Clear();
            RaiseExperienceUpdate();
        }

        // ──────────────────────────────────
        //  经验操作
        // ──────────────────────────────────

        /// <summary>
        /// 增加经验值。
        /// 返回本次增加触发的升级次数列表（供 Controller 做 RPC + 面板流程）。
        /// </summary>
        public List<int> AddExperience(int experience)
        {
            CurrentExperience += experience;

            var newLevels = CheckAndQueueUpgrades();
            RaiseExperienceUpdate();
            return newLevels;
        }

        /// <summary>
        /// 从队列取出下一个待处理的等级。
        /// 同时记录到 lastBonusCheckLevel 供 3 级奖励判断。
        /// </summary>
        public int DequeueLevelUp()
        {
            int level = _pendingLevelUps.Dequeue();
            _lastBonusCheckLevel = level;
            return level;
        }

        /// <summary>
        /// 检查并入队所有待升级。
        /// 返回本次新入队的等级列表。
        /// </summary>
        private List<int> CheckAndQueueUpgrades()
        {
            var newLevels = new List<int>();

            while (CurrentExperience >= UpgradeExperience)
            {
                int requiredExp = UpgradeExperience;

                // 执行升级（修改数据）
                CurrentExperience -= requiredExp;
                CurrentLevel++;
                UpgradeExperience = (int)(UpgradeExperience * 1.5);

                _pendingLevelUps.Enqueue(CurrentLevel);
                newLevels.Add(CurrentLevel);
            }

            return newLevels;
        }

        // ──────────────────────────────────
        //  RPC 同步入口（由 Controller 调用）
        // ──────────────────────────────────

        /// <summary>RPC 同步经验值（由 Controller 收到 RPC 后调用）</summary>
        public void SyncExperience(int syncedExp)
        {
            CurrentExperience = syncedExp;
            RaiseExperienceUpdate();
        }

        /// <summary>RPC 增加等级（由 Controller 收到 RPC 后调用）</summary>
        public void IncreaseLevel(int requiredExp)
        {
            CurrentExperience -= requiredExp;
            CurrentLevel++;
            UpgradeExperience = (int)(UpgradeExperience * 1.5);
            RaiseExperienceUpdate();
        }

        // ──────────────────────────────────
        //  直接设置经验
        // ──────────────────────────────────

        public void SetExperience(int experience)
        {
            CurrentExperience = experience;
            RaiseExperienceUpdate();
            CheckAndQueueUpgrades();
        }

        // ──────────────────────────────────
        //  经验球 ID 管理
        // ──────────────────────────────────

        public uint GenerateBallId() => _nextBallId++;

        /// <summary>尝试认领经验球，返回 false 表示已被认领（去重）</summary>
        public bool TryClaimBall(uint ballId) => _claimedBalls.Add(ballId);

        // ──────────────────────────────────
        //  专属卡牌奖励
        // ──────────────────────────────────

        /// <summary>检查并消费 3 级奖励标记</summary>
        public bool TryConsumeBonusReward()
        {
            if (_lastBonusCheckLevel > 0 && _lastBonusCheckLevel % 3 == 0)
            {
                _lastBonusCheckLevel = -1;
                return true;
            }
            _lastBonusCheckLevel = -1;
            return false;
        }

        // ──────────────────────────────────
        //  事件通知
        // ──────────────────────────────────

        private void RaiseExperienceUpdate()
        {
            if (EventChannelLocator.MainContainer?.experienceUpdateChannel == null) return;

            var data = new ExperienceUpdateData(CurrentExperience, UpgradeExperience, CurrentLevel);
            EventChannelLocator.MainContainer.experienceUpdateChannel.Raise(data);
        }
    }
}
