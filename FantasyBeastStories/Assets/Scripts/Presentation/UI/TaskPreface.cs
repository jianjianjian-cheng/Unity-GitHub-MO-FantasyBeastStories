using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Sequence = DG.Tweening.Sequence;

namespace Presentation.UI
{
    public class TaskPreface : MonoBehaviour
    {
        [Header("动画设置")]
        [SerializeField]
        private float animationDuration = 0.5f; // 动画持续时间

        [SerializeField]
        private float moveDistance = 100f; // 移动距离

        [SerializeField]
        private Ease easeType = Ease.OutQuad; // 缓动类型

        [SerializeField]
        private TextMeshProUGUI contentText;

        private CanvasGroup canvasGroup; // 用于控制透明度
        private RectTransform rectTransform; // 用于控制位置
        private Vector2 originalPosition; // 记录初始位置
        private Tween currentTween; // 当前正在播放的动画

        private void Awake()
        {
            // 获取或添加 CanvasGroup 组件
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // 获取 RectTransform
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogError("TaskPreface 需要挂载在带有 RectTransform 的 UI 对象上！");
            }

            // 记录初始位置
            originalPosition = rectTransform.anchoredPosition;
        }

        private void OnEnable()
        {
            // 可选：激活时自动播放浮现动画
            PlayTextAnimation(true);
        }

        private void OnDisable()
        {
            // 停止动画防止报错
            KillTween();
            // 隐藏 UI
            PlayTextAnimation(false);
        }

        /// <summary>
        /// 播放文字浮现/浮出动画
        /// </summary>
        /// <param name="showIn">true = 从上往下浮现（逐渐显示），false = 从下往上浮出（逐渐隐藏）</param>
        public void PlayTextAnimation(bool showIn)
        {
            // 停止当前正在播放的动画
            KillTween();

            // 设置初始状态
            if (showIn)
            {
                // 浮现：从上方开始，透明
                rectTransform.anchoredPosition = originalPosition + new Vector2(0, moveDistance);
                canvasGroup.alpha = 0f;
                gameObject.SetActive(true);

                // 向下移动到原位 + 逐渐显示
                Sequence sequence = DOTween.Sequence();
                sequence.Join(
                    rectTransform.DOAnchorPos(originalPosition, animationDuration).SetEase(easeType)
                );
                sequence.Join(canvasGroup.DOFade(1f, animationDuration).SetEase(easeType));
                currentTween = sequence;
            }
            else
            {
                // 浮出：从原位开始，向上移动 + 逐渐隐藏
                Sequence sequence = DOTween.Sequence();
                sequence.Join(
                    rectTransform
                        .DOAnchorPos(
                            originalPosition + new Vector2(0, -moveDistance),
                            animationDuration
                        )
                        .SetEase(easeType)
                );
                sequence.Join(canvasGroup.DOFade(0f, animationDuration).SetEase(easeType));
                sequence.OnComplete(() =>
                {
                    gameObject.SetActive(false);
                });
                currentTween = sequence;
            }
        }

        /// <summary>
        /// 停止当前动画
        /// </summary>
        private void KillTween()
        {
            if (currentTween != null && currentTween.IsActive())
            {
                currentTween.Kill();
                currentTween = null;
            }
        }

        /// <summary>
        /// 带回调的动画播放
        /// </summary>
        /// <param name="showIn">true = 浮现，false = 浮出</param>
        /// <param name="onComplete">动画完成回调</param>
        public void PlayTextAnimation(bool showIn, Action onComplete)
        {
            KillTween();

            if (showIn)
            {
                rectTransform.anchoredPosition = originalPosition + new Vector2(0, moveDistance);
                canvasGroup.alpha = 0f;
                gameObject.SetActive(true);

                Sequence sequence = DOTween.Sequence();
                sequence.Join(
                    rectTransform.DOAnchorPos(originalPosition, animationDuration).SetEase(easeType)
                );
                sequence.Join(canvasGroup.DOFade(1f, animationDuration).SetEase(easeType));
                sequence.OnComplete(() => onComplete?.Invoke());
                currentTween = sequence;
            }
            else
            {
                Sequence sequence = DOTween.Sequence();
                sequence.Join(
                    rectTransform
                        .DOAnchorPos(
                            originalPosition + new Vector2(0, -moveDistance),
                            animationDuration
                        )
                        .SetEase(easeType)
                );
                sequence.Join(canvasGroup.DOFade(0f, animationDuration).SetEase(easeType));
                sequence.OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    onComplete?.Invoke();
                });
                currentTween = sequence;
            }
        }

        public void SetText(string text)
        {
            contentText.text = text;
        }
    }
}