using UnityEngine;
using UnityEngine.UI;
using Domain.Time;

namespace Presentation.UI
{
    /// <summary>
    /// 时间进度条UI：显示游戏时间进度
    /// 从 SyncedGameTimeManager 获取时间数据并更新界面
    /// </summary>
    public class TimeProgressUI : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField]
        private Slider timeProgressSlider;

        [SerializeField]
        private Text timeText;

        private bool isSubscribed = false;

        void OnEnable()
        {
            TrySubscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
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

            // 如果管理器已经存在，立即刷新一次
            if (SyncedGameTimeManager.Instance != null)
            {
                UpdateTimeProgress(SyncedGameTimeManager.Instance.GetNormalizedTime() *
                                   SyncedGameTimeManager.Instance.GetTotalGameTime());
            }
        }

        void Update()
        {
            // 持续尝试订阅，直到 SyncedGameTimeManager 就绪
            if (!isSubscribed)
            {
                TrySubscribe();
            }
        }

        private void TrySubscribe()
        {
            if (isSubscribed) return;
            if (SyncedGameTimeManager.Instance == null) return;

            SyncedGameTimeManager.Instance.OnTimeUpdated += UpdateTimeProgress;
            isSubscribed = true;

            // 订阅成功时立即刷新一次
            UpdateTimeProgress(SyncedGameTimeManager.Instance.GetNormalizedTime() *
                               SyncedGameTimeManager.Instance.GetTotalGameTime());

            Debug.Log("[TimeProgressUI] 已订阅 SyncedGameTimeManager 时间更新");
        }

        private void Unsubscribe()
        {
            if (!isSubscribed) return;
            if (SyncedGameTimeManager.Instance != null)
            {
                SyncedGameTimeManager.Instance.OnTimeUpdated -= UpdateTimeProgress;
            }
            isSubscribed = false;
        }

        private void UpdateTimeProgress(float currentTime)
        {
            if (timeProgressSlider != null && SyncedGameTimeManager.Instance != null)
            {
                timeProgressSlider.value = SyncedGameTimeManager.Instance.GetNormalizedTime();
            }

            if (timeText != null && SyncedGameTimeManager.Instance != null)
            {
                float total = SyncedGameTimeManager.Instance.GetTotalGameTime();
                float remaining = SyncedGameTimeManager.Instance.GetRemainingTime();

                int curMinutes = Mathf.FloorToInt(currentTime / 60);
                int curSeconds = Mathf.FloorToInt(currentTime % 60);
                int remMinutes = Mathf.FloorToInt(remaining / 60);
                int remSeconds = Mathf.FloorToInt(remaining % 60);

                timeText.text = string.Format(
                    "{0}:{1:D2} / {2}:{3:D2}",
                    curMinutes, curSeconds, remMinutes, remSeconds
                );
            }
        }
    }
}