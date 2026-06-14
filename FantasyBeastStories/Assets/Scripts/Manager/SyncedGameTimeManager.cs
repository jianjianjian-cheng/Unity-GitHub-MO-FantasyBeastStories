using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Manager.TimeSystem;
using Manager.UI;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Manager
{
    /// <summary>
    /// 全局游戏时间管理器（支持Photon PUN2联机同步）
    /// </summary>
    public class SyncedGameTimeManager : MonoBehaviourPun, IPunObservable
    {
        [Header("时间设置")]
        [Tooltip("总游戏时间（秒）")]
        public float totalGameTime = 120f;

        [Tooltip("事件图标提前显示的时间（秒）")]
        public float markerAdvanceTime = 60f;

        [Tooltip("自动开始计时")]
        public bool autoStart = true;

        [Tooltip("是否启用网络同步（非主机/房主跟随主机时间）")]
        public bool isSynced = true;

        [Tooltip("是否循环播放（时间结束后重置）")]
        public bool loop = false;

        [Header("UI引用")]
        [Tooltip("时间进度条Slider")]
        public Slider timeSlider;

        [Tooltip("事件标记容器（Slider的Fill Area内部或重叠区域）")]
        public Transform eventMarkersContainer;

        [Tooltip("事件标记预制体")]
        public GameObject eventMarkerPrefab;

        [Tooltip("时间文本（可选）")]
        public Text timeText;

        [Header("事件列表")]
        public List<TimeEventData> timeEvents = new List<TimeEventData>();

        [SerializeField]
        private GameObject taskPreface;

        // 运行时数据
        private float currentTime = 0f;
        private bool isRunning = false;
        private HashSet<string> triggeredEventIds = new HashSet<string>();
        private Dictionary<string, GameObject> eventMarkers = new Dictionary<string, GameObject>();

        // 存储每个图标的原始颜色
        private Dictionary<string, Color> originalIconColors = new Dictionary<string, Color>();

        // 同步相关
        private float lastSyncTime = 0f;
        private float syncInterval = 0.5f;
        private double lastServerTime = 0;
        private float lastSyncedCurrentTime = 0;

        // 事件触发委托
        public Action<TimeEventData> OnTimeEventTriggered;
        public Action<float> OnTimeUpdated;
        public Action OnGameTimeFinished;
        public Action OnGameTimeLoop;

        // 单例
        public static SyncedGameTimeManager Instance { get; private set; }

        // 【修复】标记是否已初始化UI
        private bool uiInitialized = false;

        // 【修复】延迟初始化帧计数器
        private int waitFramesBeforeInit = 2;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            // 初始化Slider范围
            if (timeSlider != null)
            {
                timeSlider.minValue = 0f;
                timeSlider.maxValue = totalGameTime;
                timeSlider.wholeNumbers = false;
            }

            if (autoStart && PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_SyncStartCaTime", RpcTarget.All);
            }

            // 【修复】延迟初始化UI，等待布局完成
            StartCoroutine(DelayedInitializeUI());
        }

        //通知其他玩家游戏开始计时
        [PunRPC]
        private void RPC_SyncStartCaTime()
        {
            StartGameTime();
        }

        /// <summary>
        /// 【修复】延迟初始化UI协程
        /// </summary>
        IEnumerator DelayedInitializeUI()
        {
            // 等待2帧确保所有LayoutGroup完成布局计算
            for (int i = 0; i < waitFramesBeforeInit; i++)
            {
                yield return new WaitForEndOfFrame();
            }

            // 强制刷新布局
            Canvas.ForceUpdateCanvases();

            InitializeUI();
            uiInitialized = true;

            Debug.Log(
                $"[TimeManager] UI初始化完成，容器宽度: {(eventMarkersContainer != null ? eventMarkersContainer.GetComponent<RectTransform>().rect.width.ToString() : "null")}"
            );
        }

        void Update()
        {
            if (!isRunning)
                return;

            // 【修复】如果UI未初始化完成，跳过更新
            if (!uiInitialized)
                return;

            // 时间更新逻辑
            float deltaTime = Time.deltaTime;

            // 如果不是主机且启用了同步，时间由网络同步驱动，不本地累加
            if (isSynced && PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
            {
                return;
            }

            // 主机或非同步模式：本地累加时间
            currentTime += deltaTime;

            if (currentTime >= totalGameTime)
            {
                if (loop)
                {
                    currentTime = 0f;
                    triggeredEventIds.Clear();
                    foreach (var evt in timeEvents)
                    {
                        evt.isTriggered = false;
                    }

                    // 重置时隐藏所有图标
                    HideAllEventMarkers();

                    OnGameTimeLoop?.Invoke();

                    if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient && isSynced)
                    {
                        photonView.RPC("RPC_SyncResetTime", RpcTarget.All);
                    }
                }
                else
                {
                    currentTime = totalGameTime;
                    isRunning = false;
                    OnGameTimeFinished?.Invoke();

                    if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient && isSynced)
                    {
                        photonView.RPC("RPC_GameTimeFinished", RpcTarget.All);
                    }
                }
            }

            // 更新时间显示
            UpdateTimeSlider();
            UpdateTimeText();

            // 【修改】提前显示图标
            CheckAndShowMarkers();

            // 检查事件触发（仅主机检查并广播）
            if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient || !isSynced)
            {
                CheckAndTriggerEvents();
            }

            // 触发时间更新事件
            OnTimeUpdated?.Invoke(currentTime);

            // 通过EventManager广播
            EventManager.instance?.TriggerEventComplex(
                EventNames.GameTimeUpdated,
                new TimeEventArgs(null, currentTime)
            );
        }

        void InitializeUI()
        {
            UpdateTimeSlider();
            // 初始化Slider
            if (timeSlider != null)
            {
                timeSlider.value = currentTime;
            }

            // 清除旧标记
            if (eventMarkersContainer != null)
            {
                foreach (Transform child in eventMarkersContainer)
                {
                    Destroy(child.gameObject);
                }
            }
            eventMarkers.Clear();
            originalIconColors.Clear();

            // 【修复】先获取一次有效宽度并缓存
            float effectiveWidth = CalculateEffectiveWidth();
            Debug.Log($"[TimeManager] 计算有效宽度: {effectiveWidth}");

            // 创建事件标记（初始隐藏）
            foreach (var timeEvent in timeEvents)
            {
                CreateEventMarker(timeEvent, effectiveWidth);
            }
        }

        /// <summary>
        /// 【修复】计算Slider的有效宽度
        /// </summary>
        float CalculateEffectiveWidth()
        {
            if (timeSlider == null)
            {
                // 没有Slider时使用容器宽度
                RectTransform containerRect = eventMarkersContainer?.GetComponent<RectTransform>();
                return containerRect != null ? containerRect.rect.width : 100f;
            }

            // 方法1：优先使用Slider的fillRect实际渲染宽度
            if (timeSlider.fillRect != null)
            {
                Vector3[] fillCorners = new Vector3[4];
                timeSlider.fillRect.GetWorldCorners(fillCorners);
                float fillWidth = fillCorners[2].x - fillCorners[0].x;

                // 如果fill宽度有效（大于1像素），直接使用
                if (fillWidth > 1f)
                {
                    return fillWidth;
                }
            }

            // 方法2：使用容器RectTransform的rect.width
            if (eventMarkersContainer != null)
            {
                RectTransform containerRect = eventMarkersContainer.GetComponent<RectTransform>();
                if (containerRect.rect.width > 1f)
                {
                    return containerRect.rect.width;
                }

                // 尝试从父级获取
                RectTransform parentRect = containerRect.parent?.GetComponent<RectTransform>();
                if (parentRect != null && parentRect.rect.width > 1f)
                {
                    return parentRect.rect.width;
                }
            }

            // 方法3：使用Slider自身的RectTransform
            RectTransform sliderRect = timeSlider.GetComponent<RectTransform>();
            if (sliderRect.rect.width > 1f)
            {
                return sliderRect.rect.width;
            }

            // 方法4：使用世界坐标计算
            Vector3[] sliderCorners = new Vector3[4];
            sliderRect.GetWorldCorners(sliderCorners);
            float sliderWidth = sliderCorners[2].x - sliderCorners[0].x;
            if (sliderWidth > 1f)
            {
                return sliderWidth;
            }

            // 兜底：返回默认值
            Debug.LogWarning("[TimeManager] 无法获取有效宽度，使用默认值500");
            return 500f;
        }

        void CreateEventMarker(TimeEventData timeEvent, float effectiveWidth = -1)
        {
            if (eventMarkersContainer == null || eventMarkerPrefab == null)
                return;

            if (effectiveWidth <= 0)
            {
                effectiveWidth = CalculateEffectiveWidth();
            }

            GameObject marker = Instantiate(eventMarkerPrefab, eventMarkersContainer);
            eventMarkers[timeEvent.eventId] = marker;

            // 设置图标
            Image iconImage = marker.GetComponent<Image>();
            if (iconImage != null)
            {
                if (timeEvent.eventIcon != null)
                    iconImage.sprite = timeEvent.eventIcon;
                else if (!string.IsNullOrEmpty(timeEvent.iconResourcePath))
                {
                    Sprite loadedIcon = Resources.Load<Sprite>(timeEvent.iconResourcePath);
                    if (loadedIcon != null)
                        iconImage.sprite = loadedIcon;
                }

                Color fullOpaqueColor = timeEvent.iconColor;
                fullOpaqueColor.a = 1f;
                iconImage.color = fullOpaqueColor;
                originalIconColors[timeEvent.eventId] = fullOpaqueColor;
                iconImage.enabled = false;
            }

            RectTransform markerRect = marker.GetComponent<RectTransform>();
            RectTransform containerRect = eventMarkersContainer.GetComponent<RectTransform>();

            // 获取容器的世界坐标边界
            Vector3[] containerCorners = new Vector3[4];
            containerRect.GetWorldCorners(containerCorners);

            float containerLeftX = containerCorners[0].x; // 左边界世界X
            float containerRightX = containerCorners[2].x; // 右边界世界X
            float containerWorldWidth = containerRightX - containerLeftX;

            Debug.Log(
                $"容器世界坐标 - 左: {containerLeftX}, 右: {containerRightX}, 宽度: {containerWorldWidth}"
            );

            // 计算标记应该放置的世界X位置
            float normalizedTime = timeEvent.triggerTime / totalGameTime;
            float targetWorldX = containerLeftX + (normalizedTime * containerWorldWidth);

            // 使用世界坐标直接设置位置
            markerRect.position = new Vector3(
                targetWorldX,
                containerCorners[0].y
                    + (containerWorldWidth > 0 ? containerRect.rect.height * 0.5f : 0),
                0
            );

            Debug.Log(
                $"创建标记: {timeEvent.eventName}, 触发时间: {timeEvent.triggerTime}s, "
                    + $"归一化值: {normalizedTime:F3}, 目标世界X: {targetWorldX}, "
                    + $"实际位置: {markerRect.position}"
            );

            // 提示组件
            EventMarkerTooltip tooltip = marker.GetComponent<EventMarkerTooltip>();
            if (tooltip == null)
                tooltip = marker.AddComponent<EventMarkerTooltip>();
            tooltip.SetTooltip(
                $"{timeEvent.eventName}\n"
                    + $"触发时间: {FormatTime(timeEvent.triggerTime)}\n"
                    + $"{(markerAdvanceTime > 0 ? $"图标提前 {markerAdvanceTime}秒 显示\n" : "")}"
                    + $"{timeEvent.description}"
            );
        }

        // 【修复】公共方法：重新计算所有标记位置（用于窗口大小改变时）
        public void RecalculateMarkerPositions()
        {
            if (!uiInitialized)
                return;

            float effectiveWidth = CalculateEffectiveWidth();
            Debug.Log($"[TimeManager] 重新计算位置，有效宽度: {effectiveWidth}");

            foreach (var timeEvent in timeEvents)
            {
                if (eventMarkers.TryGetValue(timeEvent.eventId, out GameObject marker))
                {
                    RectTransform markerRect = marker.GetComponent<RectTransform>();
                    float normalizedTime = timeEvent.triggerTime / totalGameTime;
                    float positionX = normalizedTime * effectiveWidth;

                    if (timeSlider != null && timeSlider.handleRect != null)
                    {
                        float handleSize = timeSlider.handleRect.rect.width * 0.5f;
                        positionX = Mathf.Clamp(positionX, handleSize, effectiveWidth - handleSize);
                    }

                    markerRect.anchoredPosition = new Vector2(positionX, 0);
                }
            }
        }

        /// <summary>
        /// 【修改】提前显示到达时间的图标（提前markerAdvanceTime秒）
        /// </summary>
        void CheckAndShowMarkers()
        {
            if (!uiInitialized)
                return;

            foreach (var timeEvent in timeEvents)
            {
                //当前时间 >= (触发时间 - 提前量) 时就显示图标
                if (currentTime >= timeEvent.triggerTime - markerAdvanceTime)
                {
                    ShowEventMarker(timeEvent.eventId);
                    StartCoroutine(ShowTaskPreface(timeEvent.eventName));
                }
            }
        }

        private IEnumerator ShowTaskPreface(string taskName)
        {
            taskPreface.gameObject.SetActive(true);
            taskPreface.GetComponent<TextMeshProUGUI>().text = taskName + " 任务即将开始......";
            yield return new WaitForSeconds(5f);
            taskPreface.GetComponent<TaskPreface>().PlayTextAnimation(false);
            yield return new WaitForSeconds(1f);
            taskPreface.gameObject.SetActive(false);
        }

        // 显示指定事件的图标
        void ShowEventMarker(string eventId)
        {
            if (eventMarkers.TryGetValue(eventId, out GameObject marker))
            {
                Image iconImage = marker.GetComponent<Image>();
                if (iconImage != null)
                {
                    Color fullOpaqueColor = iconImage.color;
                    fullOpaqueColor.a = 1f;
                    iconImage.color = fullOpaqueColor;
                    iconImage.enabled = true;
                }
            }
        }

        // 隐藏所有事件图标
        void HideAllEventMarkers()
        {
            foreach (var marker in eventMarkers.Values)
            {
                Image iconImage = marker.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.enabled = false;
                }
            }
        }

        /// <summary>
        /// 【修改】根据当前时间显示所有应该出现的图标（提前markerAdvanceTime秒）
        /// </summary>
        void ShowMarkersUpToTime(float time)
        {
            if (!uiInitialized)
                return;

            foreach (var timeEvent in timeEvents)
            {
                // 【核心修改】提前显示图标
                if (time >= timeEvent.triggerTime - markerAdvanceTime)
                {
                    ShowEventMarker(timeEvent.eventId);
                }
                else
                {
                    if (eventMarkers.TryGetValue(timeEvent.eventId, out GameObject marker))
                    {
                        Image iconImage = marker.GetComponent<Image>();
                        if (iconImage != null)
                        {
                            iconImage.enabled = false;
                        }
                    }
                }
            }
        }

        // 更新Slider方法
        void UpdateTimeSlider()
        {
            if (timeSlider == null)
                return;
            timeSlider.value = currentTime;
        }

        void UpdateTimeBar()
        {
            UpdateTimeSlider();
        }

        void UpdateTimeText()
        {
            if (timeText != null)
            {
                timeText.text = FormatTime(currentTime);
            }
        }

        void CheckAndTriggerEvents()
        {
            if (!uiInitialized)
                return;

            foreach (var timeEvent in timeEvents)
            {
                if (
                    currentTime >= timeEvent.triggerTime
                    && !triggeredEventIds.Contains(timeEvent.eventId)
                )
                {
                    if (timeEvent.once && timeEvent.isTriggered)
                        continue;

                    TriggerEvent(timeEvent);

                    if (timeEvent.once)
                    {
                        timeEvent.isTriggered = true;
                        triggeredEventIds.Add(timeEvent.eventId);

                        if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient && isSynced)
                        {
                            photonView.RPC(
                                "RPC_OnTimeEventTriggered",
                                RpcTarget.All,
                                timeEvent.eventId,
                                currentTime
                            );
                        }
                    }
                }
            }
        }

        void TriggerEvent(TimeEventData timeEvent)
        {
            Debug.Log(
                $"[TimeManager] 事件触发: {timeEvent.eventName} 时间: {FormatTime(currentTime)}"
            );

            OnTimeEventTriggered?.Invoke(timeEvent);

            var args = new TimeEventArgs
            {
                eventData = timeEvent,
                currentTime = currentTime,
                isFromNetwork = false,
            };
            EventManager.instance?.TriggerEventComplex(EventNames.TimeEventTriggered, args);
        }

        #region PUN2 网络同步

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (!isSynced)
                return;

            if (stream.IsWriting)
            {
                stream.SendNext(currentTime);
                stream.SendNext(isRunning);
                stream.SendNext(PhotonNetwork.Time);

                string[] triggeredIds = triggeredEventIds.ToArray();
                stream.SendNext(triggeredIds.Length);
                foreach (string id in triggeredIds)
                {
                    stream.SendNext(id);
                }
            }
            else
            {
                float syncedTime = (float)stream.ReceiveNext();
                bool syncedRunning = (bool)stream.ReceiveNext();
                double serverTime = (double)stream.ReceiveNext();

                float latency = (float)(PhotonNetwork.Time - serverTime);
                float compensatedTime = syncedTime + latency;

                currentTime = Mathf.Clamp(compensatedTime, 0, totalGameTime);
                isRunning = syncedRunning;

                int triggeredCount = (int)stream.ReceiveNext();
                triggeredEventIds.Clear();
                for (int i = 0; i < triggeredCount; i++)
                {
                    string id = (string)stream.ReceiveNext();
                    triggeredEventIds.Add(id);

                    var evt = timeEvents.Find(e => e.eventId == id);
                    if (evt != null)
                        evt.isTriggered = true;

                    ShowEventMarker(id);
                }

                // 【修改】同步时使用ShowMarkersUpToTime来提前显示图标
                ShowMarkersUpToTime(currentTime);
                UpdateTimeSlider();
                UpdateTimeText();
            }
        }

        [PunRPC]
        void RPC_OnTimeEventTriggered(string eventId, float triggerTime, PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                var timeEvent = timeEvents.Find(e => e.eventId == eventId);
                if (timeEvent != null && !timeEvent.isTriggered)
                {
                    timeEvent.isTriggered = true;
                    triggeredEventIds.Add(eventId);
                    ShowEventMarker(eventId);
                    TriggerEvent(timeEvent);
                }
            }
        }

        [PunRPC]
        void RPC_GameTimeFinished(PhotonMessageInfo info)
        {
            isRunning = false;
            OnGameTimeFinished?.Invoke();
            EventManager.instance?.TriggerEvent(EventNames.GameTimeFinished);
        }

        [PunRPC]
        void RPC_SyncSetTime(float time, PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                currentTime = Mathf.Clamp(time, 0, totalGameTime);
                // 【修改】同步设置时间时提前显示图标
                ShowMarkersUpToTime(currentTime);
                UpdateTimeSlider();
                UpdateTimeText();
            }
        }

        [PunRPC]
        void RPC_SyncStartTime(PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                isRunning = true;
                EventManager.instance?.TriggerEvent(EventNames.TimeStarted);
            }
        }

        [PunRPC]
        void RPC_SyncPauseTime(PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                isRunning = false;
                EventManager.instance?.TriggerEvent(EventNames.TimePaused);
            }
        }

        [PunRPC]
        void RPC_SyncResetTime(PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                currentTime = 0f;
                triggeredEventIds.Clear();
                foreach (var evt in timeEvents)
                {
                    evt.isTriggered = false;
                }
                HideAllEventMarkers();
                UpdateTimeSlider();
                UpdateTimeText();
                EventManager.instance?.TriggerEvent(EventNames.TimeReset);
            }
        }

        #endregion

        #region 公共控制方法

        public void StartGameTime()
        {
            if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient || !isSynced)
            {
                isRunning = true;
                if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient && isSynced)
                {
                    photonView.RPC("RPC_SyncStartTime", RpcTarget.Others);
                }
                EventManager.instance?.TriggerEvent(EventNames.TimeStarted);
            }
        }

        public void PauseGameTime()
        {
            if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient || !isSynced)
            {
                isRunning = false;
                if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient && isSynced)
                {
                    photonView.RPC("RPC_SyncPauseTime", RpcTarget.Others);
                }
                EventManager.instance?.TriggerEvent(EventNames.TimePaused);
            }
        }

        public void ResumeGameTime() => StartGameTime();

        public void ResetGameTime()
        {
            if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient || !isSynced)
            {
                currentTime = 0f;
                triggeredEventIds.Clear();
                foreach (var evt in timeEvents)
                {
                    evt.isTriggered = false;
                }
                HideAllEventMarkers();
                UpdateTimeSlider();
                UpdateTimeText();

                if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient && isSynced)
                {
                    photonView.RPC("RPC_SyncResetTime", RpcTarget.All);
                }
                EventManager.instance?.TriggerEvent(EventNames.TimeReset);
            }
        }

        public void SetTime(float time)
        {
            time = Mathf.Clamp(time, 0f, totalGameTime);
            if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient || !isSynced)
            {
                currentTime = time;
                // 【修改】设置时间时提前显示图标
                ShowMarkersUpToTime(currentTime);
                UpdateTimeSlider();
                UpdateTimeText();
                if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient && isSynced)
                {
                    photonView.RPC("RPC_SyncSetTime", RpcTarget.Others, time);
                }
            }
        }

        public void AddTimeEvent(TimeEventData timeEvent)
        {
            timeEvents.Add(timeEvent);

            // 确保使用当前有效宽度
            float effectiveWidth = CalculateEffectiveWidth();
            CreateEventMarker(timeEvent, effectiveWidth);

            // 【修改】如果当前时间已经接近触发时间（提前量内），也显示图标
            if (currentTime >= timeEvent.triggerTime - markerAdvanceTime)
            {
                ShowEventMarker(timeEvent.eventId);
            }
        }

        public void RemoveTimeEvent(string eventId)
        {
            var evt = timeEvents.Find(e => e.eventId == eventId);
            if (evt != null)
            {
                timeEvents.Remove(evt);
                if (eventMarkers.TryGetValue(eventId, out GameObject marker))
                {
                    Destroy(marker);
                    eventMarkers.Remove(eventId);
                    originalIconColors.Remove(eventId);
                }
            }
        }

        public float GetCurrentTime() => currentTime;

        public float GetNormalizedTime() => currentTime / totalGameTime;

        public bool IsTimeRunning() => isRunning;

        public float GetRemainingTime() => Mathf.Max(0, totalGameTime - currentTime);

        public string FormatTime(float seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            if (totalGameTime >= 3600)
                return string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
            else
                return string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
        }

        #endregion

        #region 调试方法

        [ContextMenu("Debug Marker Positions")]
        public void DebugMarkerPositions()
        {
            float effectiveWidth = CalculateEffectiveWidth();
            Debug.Log($"=== 标记位置调试信息 ===");
            Debug.Log($"当前有效宽度: {effectiveWidth}");
            Debug.Log($"UI初始化状态: {uiInitialized}");

            foreach (var kvp in eventMarkers)
            {
                var timeEvent = timeEvents.Find(e => e.eventId == kvp.Key);
                if (timeEvent != null)
                {
                    RectTransform markerRect = kvp.Value.GetComponent<RectTransform>();
                    float expectedX = (timeEvent.triggerTime / totalGameTime) * effectiveWidth;

                    Debug.Log(
                        $"标记: {timeEvent.eventName}\n"
                            + $"  触发时间: {timeEvent.triggerTime}s\n"
                            + $"  期望X位置: {expectedX:F2}\n"
                            + $"  实际位置: {markerRect.anchoredPosition}"
                    );
                }
            }
        }

        [ContextMenu("Recalculate All Marker Positions")]
        public void RecalculateAllMarkerPositions()
        {
            RecalculateMarkerPositions();
        }

        #endregion
    }
}
