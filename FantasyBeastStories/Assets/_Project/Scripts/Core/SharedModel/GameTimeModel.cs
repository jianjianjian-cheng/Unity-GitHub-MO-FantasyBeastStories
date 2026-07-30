using System;
using System.Collections.Generic;

namespace Core.SharedModel
{
    /// <summary>
    /// 游戏时间模型 — 纯 C# 类，不继承 MonoBehaviour，可独立单元测试。
    ///
    /// 持有：
    /// - 当前时间 / 总时间 / 运行状态
    /// - 已触发事件 ID 集合
    /// - 事件列表（运行时副本，支持增删）
    /// - Boss 生成标记
    ///
    /// 外部依赖（RPC / NetworkServiceLocator / Time.deltaTime / EventChannelSO）
    /// 由 Controller 处理，Model 只管理数据与回调通知。
    /// </summary>
    public class GameTimeModel
    {
        // ──────────────────────────────────
        //  时间状态
        // ──────────────────────────────────

        public float CurrentTime { get; private set; }
        public float TotalGameTime { get; }
        public bool IsRunning { get; private set; }
        public bool Loop { get; }

        // ──────────────────────────────────
        //  事件触发状态
        // ──────────────────────────────────

        private readonly HashSet<string> _triggeredEventIds = new();

        // ──────────────────────────────────
        //  Boss 状态
        // ──────────────────────────────────

        public bool IsBossGenerated { get; private set; }
        public string BossName { get; }

        // ──────────────────────────────────
        //  事件
        // ──────────────────────────────────

        public event Action<TimeEventData> OnTimeEventTriggered;
        public event Action<float> OnTimeUpdated;
        public event Action OnGameTimeFinished;
        public event Action OnGameTimeLoop;
        public event Action<string> OnBossSpawn;
        public event Action<float> OnEnemyAttributeChange;

        /// <summary>运行时事件列表（从 SO 加载副本）</summary>
        public List<TimeEventData> TimeEvents { get; } = new();

        private float _lastEnemyAttrTriggerTime;

        public GameTimeModel(float totalGameTime, bool loop, string bossName)
        {
            TotalGameTime = totalGameTime;
            Loop = loop;
            BossName = bossName;
        }

        // ──────────────────────────────────
        //  时间控制
        // ──────────────────────────────────

        public void Start() => IsRunning = true;
        public void Pause() => IsRunning = false;
        public void Resume() => IsRunning = true;

        /// <summary>
        /// 推进时间。返回需要由 Controller 处理的网络同步操作列表。
        /// </summary>
        /// <param name="deltaTime">本次增量</param>
        /// <returns>需要网络同步的操作（null 表示无需同步）</returns>
        public TimeSyncOp AdvanceTime(float deltaTime)
        {
            if (!IsRunning)
                return TimeSyncOp.None;

            CurrentTime += deltaTime;

            OnTimeUpdated?.Invoke(CurrentTime);

            // 检查结束
            if (CurrentTime >= TotalGameTime)
            {
                if (Loop)
                {
                    Reset();
                    OnGameTimeLoop?.Invoke();
                    return TimeSyncOp.Loop;
                }
                else
                {
                    CurrentTime = TotalGameTime;
                    IsRunning = false;
                    OnGameTimeFinished?.Invoke();
                    return TimeSyncOp.Finished;
                }
            }

            // 检查事件触发
            CheckAndTriggerEvents();

            // 每隔 60 秒触发敌人属性变化
            if (CurrentTime - _lastEnemyAttrTriggerTime >= 60f)
            {
                _lastEnemyAttrTriggerTime = CurrentTime;
                OnEnemyAttributeChange?.Invoke(CurrentTime);
            }

            // 生成最终 Boss
            if (CurrentTime >= TotalGameTime - 900f && !IsBossGenerated)
            {
                IsBossGenerated = true;
                OnBossSpawn?.Invoke(BossName);
                return TimeSyncOp.BossSpawn;
            }

            return TimeSyncOp.None;
        }

        private void CheckAndTriggerEvents()
        {
            foreach (var timeEvent in TimeEvents)
            {
                if (CurrentTime >= timeEvent.triggerTime
                    && !_triggeredEventIds.Contains(timeEvent.eventId))
                {
                    if (timeEvent.once && timeEvent.isTriggered)
                        continue;

                    TriggerEvent(timeEvent);

                    if (timeEvent.once)
                    {
                        timeEvent.isTriggered = true;
                        _triggeredEventIds.Add(timeEvent.eventId);
                    }
                }
            }
        }

        private void TriggerEvent(TimeEventData timeEvent)
        {
            OnTimeEventTriggered?.Invoke(timeEvent);
        }

        // ──────────────────────────────────
        //  网络同步入口（由 Controller 收到 RPC 后调用）
        // ──────────────────────────────────

        /// <summary>非主机端：标记事件已触发（RPC 同步）</summary>
        public void MarkEventTriggered(string eventId)
        {
            var timeEvent = TimeEvents.Find(e => e.eventId == eventId);
            if (timeEvent != null && !timeEvent.isTriggered)
            {
                timeEvent.isTriggered = true;
                _triggeredEventIds.Add(eventId);
                TriggerEvent(timeEvent);
            }
        }

        /// <summary>非主机端：时间结束</summary>
        public void FinishFromNetwork()
        {
            IsRunning = false;
            OnGameTimeFinished?.Invoke();
        }

        /// <summary>非主机端：Boss 生成</summary>
        public void BossSpawnFromNetwork(string bossName)
        {
            IsBossGenerated = true;
            OnBossSpawn?.Invoke(bossName);
        }

        /// <summary>非主机端：同步设置时间</summary>
        public void SetTimeFromNetwork(float time)
        {
            CurrentTime = UnityEngine.Mathf.Clamp(time, 0, TotalGameTime);
            OnTimeUpdated?.Invoke(CurrentTime);
        }

        /// <summary>非主机端：同步开始</summary>
        public void StartFromNetwork()
        {
            IsRunning = true;
            OnTimeUpdated?.Invoke(CurrentTime);
        }

        /// <summary>非主机端：同步暂停</summary>
        public void PauseFromNetwork()
        {
            IsRunning = false;
            OnTimeUpdated?.Invoke(CurrentTime);
        }

        // ──────────────────────────────────
        //  重置
        // ──────────────────────────────────

        public void Reset()
        {
            CurrentTime = 0f;
            _triggeredEventIds.Clear();
            _lastEnemyAttrTriggerTime = 0f;
            foreach (var evt in TimeEvents)
                evt.isTriggered = false;
        }

        public void SetTime(float time)
        {
            CurrentTime = UnityEngine.Mathf.Clamp(time, 0, TotalGameTime);
        }

        // ──────────────────────────────────
        //  事件列表管理
        // ──────────────────────────────────

        public void LoadEvents(List<TimeEventData> events)
        {
            TimeEvents.Clear();
            TimeEvents.AddRange(events);
        }

        public void AddTimeEvent(TimeEventData timeEvent) => TimeEvents.Add(timeEvent);

        public void RemoveTimeEvent(string eventId)
        {
            var evt = TimeEvents.Find(e => e.eventId == eventId);
            if (evt != null) TimeEvents.Remove(evt);
        }

        // ──────────────────────────────────
        //  查询
        // ──────────────────────────────────

        public float GetNormalizedTime() => CurrentTime / TotalGameTime;
        public float GetRemainingTime() => UnityEngine.Mathf.Max(0, TotalGameTime - CurrentTime);

        public string FormatTime(float seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            if (TotalGameTime >= 3600)
                return string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
            else
                return string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
        }
    }

    /// <summary>
    /// 时间推进后需要 Controller 执行的网络同步操作类型。
    /// </summary>
    public enum TimeSyncOp
    {
        None,
        Loop,
        Finished,
        BossSpawn
    }
}
