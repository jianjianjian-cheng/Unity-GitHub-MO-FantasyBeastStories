using Domain.Event;
using Domain.Event.Channels.Game;
using Domain.Time.TimeSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Presentation.UI
{
    /// <summary>
    /// 时间进度条UI：显示游戏时间进度
    /// 通过 TimeEventChannelSO 订阅时间更新，不直接依赖 Domain 层 MonoBehaviour
    /// </summary>
    public class TimeProgressUI : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField]
        private Slider timeProgressSlider;

        [SerializeField]
        private Text timeText;

        private float totalGameTime = 0f;
        private bool isSubscribed = false;
        private bool isInitialized = false;

        void OnEnable()
        {
            SubscribeToTimeChannel();
        }

        void OnDisable()
        {
            UnsubscribeFromTimeChannel();
        }

        void Start()
        {
            // 未赋值时自动查找
            if (timeProgressSlider == null)
                timeProgressSlider = GetComponent<Slider>();

            if (timeText == null)
                timeText = GetComponentInChildren<Text>();

            // 初始化进度条范围
            if (timeProgressSlider != null)
            {
                timeProgressSlider.minValue = 0f;
                timeProgressSlider.maxValue = 1f;
            }
        }

        void Update()
        {
            if (!isSubscribed)
            {
                SubscribeToTimeChannel();
            }
            if (!isInitialized)
            {
                QueryTotalGameTime();
            }
        }

        private void SubscribeToTimeChannel()
        {
            if (isSubscribed) return;
            if (EventChannelLocator.MainContainer?.timeEventChannel == null) return;

            EventChannelLocator.MainContainer.timeEventChannel.RegisterListener(OnTimeUpdated);
            isSubscribed = true;

            Debug.Log("[TimeProgressUI] 已订阅 TimeEventChannelSO");
        }

        private void UnsubscribeFromTimeChannel()
        {
            if (!isSubscribed) return;
            if (EventChannelLocator.MainContainer?.timeEventChannel != null)
            {
                EventChannelLocator.MainContainer.timeEventChannel.UnregisterListener(OnTimeUpdated);
            }
            isSubscribed = false;
        }

        private void QueryTotalGameTime()
        {
            if (EventChannelLocator.MainContainer?.timeQueryChannel == null) return;

            var query = new TimeQueryData();
            EventChannelLocator.MainContainer.timeQueryChannel.Query(query);
            if (query.totalGameTime > 0f)
            {
                totalGameTime = query.totalGameTime;
                isInitialized = true;
            }
        }

        private void OnTimeUpdated(TimeEventArgs args)
        {
            UpdateTimeDisplay(args.currentTime);
        }

        private void UpdateTimeDisplay(float currentTime)
        {
            float normalizedTime = totalGameTime > 0f ? currentTime / totalGameTime : 0f;
            float remainingTime = Mathf.Max(0f, totalGameTime - currentTime);

            if (timeProgressSlider != null)
            {
                timeProgressSlider.value = normalizedTime;
            }

            if (timeText != null)
            {
                int curMinutes = Mathf.FloorToInt(currentTime / 60);
                int curSeconds = Mathf.FloorToInt(currentTime % 60);

                timeText.text = string.Format("{0}:{1:D2}", curMinutes, curSeconds);
            }
        }
    }
}