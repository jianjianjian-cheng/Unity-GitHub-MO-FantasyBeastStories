using Domain.Event;
using Domain.Event.Channels.Game;
using Domain.Time.TimeSystem;
using Presentation.UI.Framework.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Presentation.UI
{
    /// <summary>
    /// 对局时间进度条 Widget
    /// 默认显示 → Boss 出现后隐藏（焦点转移到 Boss 血条）
    ///
    /// 使用方式：
    ///   挂载到时间进度条 UI 根节点上，UIWidget 框架自动管理生命周期。
    /// </summary>
    public class TimeProgressWidget : UIWidget
    {
        [Header("UI引用")]
        [SerializeField] private Slider timeProgressSlider;
        [SerializeField] private TextMeshProUGUI timeText;

        // 用 CanvasGroup 控制显隐，不阻塞事件订阅
        private CanvasGroup _canvasGroup;

        // 时间数据
        private float _totalGameTime;
        private bool _hasTotalTime;

        // 订阅重试标记（事件通道可能因初始化顺序暂未就绪）
        private bool _hasSubscribed;

        // ──────────────────────────────────────────────
        //  AutoBindComponents：自动查找子组件
        // ──────────────────────────────────────────────

        protected override void AutoBindComponents()
        {
            if (timeProgressSlider == null)
                timeProgressSlider = GetComponentInChildren<Slider>(true);

            if (timeText == null)
                timeText = GetComponentInChildren<TextMeshProUGUI>(true);

            // 初始化 Slider 范围
            if (timeProgressSlider != null)
            {
                timeProgressSlider.minValue = 0f;
                timeProgressSlider.maxValue = 1f;
                timeProgressSlider.value = 0f;
            }

            if (timeText != null)
                timeText.text = "";

            // CanvasGroup 控制显隐（初始显示）
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }

        // ──────────────────────────────────────────────
        //  事件订阅 / 取消（含 Update 重试机制）
        // ──────────────────────────────────────────────

        protected override void SubscribeEvents()
        {
            if (_hasSubscribed) return;
            if (EventChannelLocator.MainContainer == null) return;

            bool bossReady = EventChannelLocator.MainContainer.bossSpawnChannel != null;
            bool timeReady = EventChannelLocator.MainContainer.timeEventChannel != null;

            if (bossReady)
                EventChannelLocator.MainContainer.bossSpawnChannel.RegisterListener(OnBossSpawned);
            if (timeReady)
                EventChannelLocator.MainContainer.timeEventChannel.RegisterListener(OnTimeUpdated);

            if (bossReady && timeReady)
                _hasSubscribed = true;
        }

        protected override void UnsubscribeEvents()
        {
            if (!_hasSubscribed) return;
            if (EventChannelLocator.MainContainer != null)
            {
                if (EventChannelLocator.MainContainer.bossSpawnChannel != null)
                    EventChannelLocator.MainContainer.bossSpawnChannel.UnregisterListener(OnBossSpawned);
                if (EventChannelLocator.MainContainer.timeEventChannel != null)
                    EventChannelLocator.MainContainer.timeEventChannel.UnregisterListener(OnTimeUpdated);
            }
            _hasSubscribed = false;
        }

        /// <summary>
        /// Update 重试：每帧重试订阅 & 查询总时间，直到就绪
        /// </summary>
        private void Update()
        {
            if (!_hasSubscribed)
                SubscribeEvents();

            if (!_hasTotalTime)
                QueryTotalGameTime();
        }

        // ──────────────────────────────────────────────
        //  事件回调
        // ──────────────────────────────────────────────

        /// <summary>
        /// 查询总游戏时间（totalGameTime）
        /// </summary>
        private void QueryTotalGameTime()
        {
            if (EventChannelLocator.MainContainer?.timeQueryChannel == null) return;

            var query = new TimeQueryData();
            EventChannelLocator.MainContainer.timeQueryChannel.Query(query);
            if (query.totalGameTime > 0f)
            {
                _totalGameTime = query.totalGameTime;
                _hasTotalTime = true;
            }
        }

        /// <summary>
        /// Boss 生成 → 隐藏对局时间条（焦点转到 Boss 血条）
        /// </summary>
        private void OnBossSpawned(string bossName)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// 游戏时间更新 → 刷新进度条与文本
        /// </summary>
        private void OnTimeUpdated(TimeEventArgs args)
        {
            UpdateTimeDisplay(args.currentTime);
        }

        private void UpdateTimeDisplay(float currentTime)
        {
            float normalizedTime = _hasTotalTime && _totalGameTime > 0f
                ? currentTime / _totalGameTime
                : 0f;
            float remainingTime = Mathf.Max(0f, (_hasTotalTime ? _totalGameTime : 0f) - currentTime);

            if (timeProgressSlider != null)
            {
                timeProgressSlider.value = normalizedTime;
            }

            if (timeText != null)
            {
                int minutes = Mathf.FloorToInt(remainingTime / 60);
                int seconds = Mathf.FloorToInt(remainingTime % 60);
                timeText.text = $"{minutes}:{seconds:D2}";
            }
        }
    }
}