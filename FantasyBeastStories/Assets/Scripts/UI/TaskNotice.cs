using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Sequence = DG.Tweening.Sequence;

public class TaskNotice : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI name;

    [SerializeField]
    private TextMeshProUGUI description;

    [SerializeField]
    private TextMeshProUGUI data;

    [Header("动画设置")]
    [SerializeField]
    private float animationDuration = 0.5f; // 动画持续时间

    [SerializeField]
    private float moveDistance = 200f; // 移动距离

    [SerializeField]
    private Ease easeType = Ease.OutQuad; // 缓动类型

    private CanvasGroup canvasGroup; // 用于控制透明度
    private RectTransform rectTransform; // 用于控制位置
    private Vector2 originalPosition; // 记录初始位置
    private Tween currentTween; // 当前正在播放的动画

    void Awake()
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
            Debug.LogError("TaskNotice 需要挂载在带有 RectTransform 的 UI 对象上！");
        }

        // 记录初始位置
        originalPosition = rectTransform.anchoredPosition;
    }

    void Start() { }

    void Update() { }

    public void SetInfo(string name, string description, string data)
    {
        this.name.text = name;
        this.description.text = description;
        this.data.text = data;
    }

    public void Notice_Data(string data)
    {
        this.data.text = data;
    }

    private void OnEnable()
    {
        PlaySlideAnimation(true);
    }

    private void OnDisable()
    {
        // 停止动画防止报错
        KillTween();
    }

    /// <summary>
    /// 播放文字滑入/滑出动画
    /// </summary>
    /// <param name="slideIn">true = 从右往左滑入（逐渐显示），false = 从左往右滑出（逐渐隐藏）</param>
    public void PlaySlideAnimation(bool slideIn)
    {
        // 停止当前正在播放的动画
        KillTween();

        // 设置初始状态
        if (slideIn)
        {
            // 滑入：从右边开始，透明
            rectTransform.anchoredPosition = originalPosition + new Vector2(moveDistance, 0);
            canvasGroup.alpha = 0f;
            gameObject.SetActive(true);

            // 向左移动到原位 + 逐渐显示
            Sequence sequence = DOTween.Sequence();
            sequence.Join(
                rectTransform.DOAnchorPos(originalPosition, animationDuration).SetEase(easeType)
            );
            sequence.Join(canvasGroup.DOFade(1f, animationDuration).SetEase(easeType));
            currentTween = sequence;
        }
        else
        {
            // 滑出：从原位向右移动 + 逐渐隐藏
            Sequence sequence = DOTween.Sequence();
            sequence.Join(
                rectTransform
                    .DOAnchorPos(originalPosition + new Vector2(moveDistance, 0), animationDuration)
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
    /// <param name="slideIn">true = 从右往左滑入，false = 从左往右滑出</param>
    /// <param name="onComplete">动画完成回调</param>
    public void PlaySlideAnimation(bool slideIn, Action onComplete)
    {
        KillTween();

        if (slideIn)
        {
            rectTransform.anchoredPosition = originalPosition + new Vector2(moveDistance, 0);
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
                    .DOAnchorPos(originalPosition + new Vector2(moveDistance, 0), animationDuration)
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
}
