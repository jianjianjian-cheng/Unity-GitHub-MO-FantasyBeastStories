using System.Collections;
using Core;
using DG.Tweening;
using TMPro;
using UI.Framework.Base;
using UnityEngine;

namespace UI.Framework
{
    /// <summary>
    /// 顶部消息通知 Widget
    ///
    /// 职责：
    /// - 收到消息后用 DOTween 滑入，停留指定时间后通过 Animator trigger "Close" 播放关闭动画
    /// - 自动监听：符文购买成功 / 任务完成 / Boss击杀完成
    ///
    /// 挂在 Top 层 Canvas 上作为常驻 Widget
    /// </summary>
    public class TopNotice : UIWidget
    {
        [Header("TopNotice UI")]
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Animator animator;

        [Header("TopNotice 动画设置")]
        [Tooltip("消息停留时间（秒）")]
        [SerializeField] private float displayDuration = 2.5f;
        [Tooltip("滑入动画时长（秒）")]
        [SerializeField] private float animDuration = 0.4f;
        [Tooltip("滑入距离（从上方偏移多少滑入）")]
        [SerializeField] private float slideDistance = 100f;

        private static readonly int CloseHash = Animator.StringToHash("Close");

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private Vector2 _originalPosition;

        private bool _isShowing;
        private Coroutine _autoHideCoroutine;
        private Tween _openTween;
        private Tween _hideTween;

        // ──────────────────────────────────────────────
        //  AutoBindComponents
        // ──────────────────────────────────────────────

        protected override void AutoBindComponents()
        {
            if (messageText == null)
                messageText = FindComponentInChildren<TextMeshProUGUI>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            _rectTransform = GetComponent<RectTransform>();
            _originalPosition = _rectTransform.anchoredPosition;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // 添加独立 Canvas 覆盖排序，确保始终渲染在最高层
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 9999;

            SetVisible(false);
        }

        // ──────────────────────────────────────────────
        //  事件订阅 / 取消
        // ──────────────────────────────────────────────

        protected override void SubscribeEvents()
        {
            var container = EventChannelLocator.MainContainer;
            if (container == null) return;

            if (container.shopEventChannel != null)
                container.shopEventChannel.RegisterListener(OnRunePurchased);

            if (container.bossDeathChannel != null)
                container.bossDeathChannel.RegisterListener(OnBossDeath);

            if (container.taskUIChannel != null)
                container.taskUIChannel.RegisterListener(OnTaskUIEvent);
        }

        protected override void UnsubscribeEvents()
        {
            var container = EventChannelLocator.MainContainer;
            if (container == null) return;

            if (container.shopEventChannel != null)
                container.shopEventChannel.UnregisterListener(OnRunePurchased);

            if (container.bossDeathChannel != null)
                container.bossDeathChannel.UnregisterListener(OnBossDeath);

            if (container.taskUIChannel != null)
                container.taskUIChannel.UnregisterListener(OnTaskUIEvent);
        }

        // ──────────────────────────────────────────────
        //  事件回调
        // ──────────────────────────────────────────────

        private void OnRunePurchased(RunePurchasedEventData data)
        {
            Show("购买成功");
        }

        private void OnBossDeath()
        {
            Show("击杀完成");
        }

        private void OnTaskUIEvent(TaskUIUpdateData data)
        {
            if (data.eventType == TaskUIEventType.NoticeData)
            {
                if (data.data != null && data.data.Contains("任务完成"))
                    Show("任务完成");
            }
        }

        // ──────────────────────────────────────────────
        //  公共 API
        // ──────────────────────────────────────────────

        /// <summary>显示一条顶部通知</summary>
        public void Show(string message)
        {
            if (messageText != null)
                messageText.text = message;

            if (_autoHideCoroutine != null)
                StopCoroutine(_autoHideCoroutine);

            KillOpenTween();
            _isShowing = true;
            SetVisible(true);

            // 初始状态：上方偏移 + 透明
            _rectTransform.anchoredPosition = _originalPosition + new Vector2(0, slideDistance);
            _canvasGroup.alpha = 0f;

            // DOTween 滑入动画：位移归位 + 淡入
            _openTween = DOTween.Sequence()
                .Join(_rectTransform.DOAnchorPos(_originalPosition, animDuration).SetEase(Ease.OutQuad))
                .Join(_canvasGroup.DOFade(1f, animDuration).SetEase(Ease.OutQuad));

            _autoHideCoroutine = StartCoroutine(AutoHideAfter(displayDuration));
        }

        /// <summary>手动隐藏通知（Animator Close + DOTween 逐渐透明）</summary>
        public void Hide()
        {
            if (!_isShowing) return;

            if (_autoHideCoroutine != null)
            {
                StopCoroutine(_autoHideCoroutine);
                _autoHideCoroutine = null;
            }

            KillOpenTween();
            KillHideTween();
            _isShowing = false;

            // Animator 触发 Close 状态转换
            if (animator != null)
                animator.SetTrigger(CloseHash);

            // DOTween 同时逐渐变透明
            _hideTween = _canvasGroup.DOFade(0f, animDuration).SetEase(Ease.InQuad)
                .OnComplete(() => SetVisible(false));
        }

        // ──────────────────────────────────────────────
        //  内部协程
        // ──────────────────────────────────────────────

        private IEnumerator AutoHideAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            Hide();
        }

        // ──────────────────────────────────────────────
        //  工具方法
        // ──────────────────────────────────────────────

        private void SetVisible(bool visible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.blocksRaycasts = visible;
                _canvasGroup.interactable = visible;
            }
        }

        private void KillOpenTween()
        {
            if (_openTween != null && _openTween.IsActive())
            {
                _openTween.Kill();
                _openTween = null;
            }
        }

        private void KillHideTween()
        {
            if (_hideTween != null && _hideTween.IsActive())
            {
                _hideTween.Kill();
                _hideTween = null;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            KillOpenTween();
            KillHideTween();
        }
    }
}
