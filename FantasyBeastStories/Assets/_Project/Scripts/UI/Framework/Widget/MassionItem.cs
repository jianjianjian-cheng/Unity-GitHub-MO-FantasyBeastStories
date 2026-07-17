using System.Collections;
using UI.Framework.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 任务项 Widget — 对应 ScrollView 中的单个任务条目。
///
/// Inspector 需绑定：
/// - iconImage         : 任务图标
/// - completedIcon     : 已完成图标（默认隐藏，完成后显示）
/// - descriptionText   : 任务描述
/// - progressBarBg     : 进度条背景
/// - progressBarFill   : 进度条填充
/// - progressText      : 详细进度文字（如 "10/30"）
///
/// 进度条实现方式：
/// 使用 RectTransform.width 控制填充宽度，
/// Fill 的 Pivot 需为 (0, 0.5)，以便从左向右扩展。
/// 注意：代码会自动设置 Pivot，无需在 Inspector 手动配置。
///
/// 平滑过渡：
/// - AnimateToTarget() 驱动填充宽度和数字从 0→目标值
/// - 完成后自动显示 completedIcon
/// - 已完成任务跳过动画，直接展示完成状态
/// </summary>
public class MassionItem : UIWidget
{
    [Header("任务项 UI 组件")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image completedIcon;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image progressBarBg;
    [SerializeField] private Image progressBarFill;
    [SerializeField] private TextMeshProUGUI progressText;

    // ── 运行时数据 ──
    private int taskId;
    private QuestTaskSO taskData;
    private int currentCount;
    private int targetCount = 1;

    // ── 进度条缓存 ──
    private RectTransform fillRectTransform;
    private float fullBarWidth;

    // ── 只读属性 ──
    public int TaskId => taskId;
    public QuestTaskSO TaskData => taskData;
    public int CurrentCount => currentCount;
    public int TargetCount => targetCount;

    // ──────────────────────────────────────────────
    //  AutoBindComponents
    // ──────────────────────────────────────────────

    protected override void AutoBindComponents()
    {
        // 自动查找 Icon
        if (iconImage == null)
        {
            var tr = transform.Find("Icon");
            if (tr != null) iconImage = tr.GetComponent<Image>();
        }

        // 自动查找 CompletedIcon
        if (completedIcon == null)
        {
            var tr = transform.Find("CompletedIcon");
            if (tr != null) completedIcon = tr.GetComponent<Image>();
        }

        // 自动查找 Description
        if (descriptionText == null)
            descriptionText = GetComponentInChildren<TextMeshProUGUI>();

        // 自动查找进度条背景
        if (progressBarBg == null)
        {
            var tr = transform.Find("ProgressBar");
            if (tr != null) progressBarBg = tr.GetComponent<Image>();
        }

        // 自动查找进度条填充（Fill）
        if (progressBarFill == null)
        {
            var barTr = transform.Find("ProgressBar");
            if (barTr != null)
            {
                var fillTr = barTr.Find("Fill");
                if (fillTr != null) progressBarFill = fillTr.GetComponent<Image>();
            }
        }

        // 缓存 Fill 的 RectTransform 并配置 Pivot
        if (progressBarFill != null)
        {
            fillRectTransform = progressBarFill.rectTransform;
            // 设置 Pivot 为 (0, 0.5)，确保只向右扩展
            fillRectTransform.pivot = new Vector2(0f, 0.5f);
            fillRectTransform.anchorMin = new Vector2(0f, 0f);
            fillRectTransform.anchorMax = new Vector2(0f, 1f);
            fillRectTransform.anchoredPosition = Vector2.zero;
        }

        // 自动查找 ProgressText
        if (progressText == null)
        {
            var tr = transform.Find("ProgressText");
            if (tr != null) progressText = tr.GetComponent<TextMeshProUGUI>();
        }

        // 初始状态：已完成图标隐藏
        if (completedIcon != null)
            completedIcon.gameObject.SetActive(false);

        // 关闭所有图片的射线检测，避免挡住 ScrollView 滚动
        foreach (var img in GetComponentsInChildren<Image>(true))
            img.raycastTarget = false;
    }

    // ──────────────────────────────────────────────
    //  数据注入
    // ──────────────────────────────────────────────

    /// <summary>
    /// 设置任务数据，显示为初始状态（进度归零）。
    /// 之后调用 AnimateToTarget() 播放平滑过渡。
    /// </summary>
    public void Setup(QuestTaskSO data)
    {
        taskData = data;

        if (data == null)
        {
            ClearDisplay();
            return;
        }

        taskId = data.taskId;
        targetCount = data.targetCount;
        currentCount = 0;

        // 缓存进度条完整宽度（背景的宽度）
        if (progressBarBg != null)
            fullBarWidth = progressBarBg.rectTransform.rect.width;

        // 设置静态内容
        if (iconImage != null)
            iconImage.sprite = data.icon;

        if (descriptionText != null)
            descriptionText.text = data.taskDescription;

        // 初始显示为 0/目标
        SetFillWidth(0f);

        if (progressText != null)
            progressText.text = $"0/{targetCount}";

        if (completedIcon != null)
            completedIcon.gameObject.SetActive(false);
    }

    /// <summary>
    /// 设置初始旧进度（不触发完成动画，只显示已有进度）。
    /// 调用后再调用 AnimateToTarget() 会从 oldVal 开始动画到新值。
    /// </summary>
    public void SetInitialProgress(int oldVal)
    {
        currentCount = Mathf.Max(0, oldVal);
        float ratio = targetCount > 0 ? (float)currentCount / targetCount : 0f;
        SetFillWidth(Mathf.Min(ratio, 1f));

        if (progressText != null)
            progressText.text = $"{currentCount}/{targetCount}";

        // 不显示完成图标，等待动画结束或 SetFinalProgress
        if (completedIcon != null)
            completedIcon.gameObject.SetActive(false);
    }

    /// <summary>
    /// 直接设置最终进度状态（无动画），用于已完成的任务。
    /// </summary>
    public void SetFinalProgress(int progress)
    {
        currentCount = progress;
        targetCount = taskData != null ? taskData.targetCount : 1;

        float ratio = targetCount > 0 ? (float)currentCount / targetCount : 0f;
        SetFillWidth(Mathf.Min(ratio, 1f));

        if (progressText != null)
            progressText.text = $"{currentCount}/{targetCount}";

        // 已完成显示图标
        if (completedIcon != null)
            completedIcon.gameObject.SetActive(currentCount >= targetCount);
    }

    private void ClearDisplay()
    {
        taskId = -1;
        targetCount = 1;
        currentCount = 0;

        if (iconImage != null) iconImage.sprite = null;
        if (descriptionText != null) descriptionText.text = string.Empty;
        SetFillWidth(0f);
        if (progressText != null) progressText.text = "0/1";
        if (completedIcon != null) completedIcon.gameObject.SetActive(false);
    }

    // ──────────────────────────────────────────────
    //  进度条宽度控制（核心）
    // ──────────────────────────────────────────────

    /// <summary>根据比例（0~1）设置 Fill 的宽度</summary>
    private void SetFillWidth(float ratio)
    {
        if (fillRectTransform == null) return;
        float width = Mathf.Clamp01(ratio) * fullBarWidth;
        fillRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }

    // ──────────────────────────────────────────────
    //  平滑过渡动画
    // ──────────────────────────────────────────────

    /// <summary>
    /// 从 0 播放平滑过渡到目标进度。
    /// </summary>
    /// <param name="toValue">目标进度值</param>
    /// <param name="duration">动画时长（秒）</param>
    public void AnimateToTarget(int toValue, float duration = 0.8f)
    {
        // 如果已处于完成状态或目标为 0，直接跳转
        if (currentCount >= targetCount || toValue <= 0)
        {
            SetFinalProgress(toValue);
            return;
        }

        StartCoroutine(AnimateProgressCoroutine(toValue, duration));
    }

    private IEnumerator AnimateProgressCoroutine(int toValue, float duration)
    {
        int fromValue = currentCount;
        float elapsed = 0f;
        float fromRatio = targetCount > 0 ? (float)fromValue / targetCount : 0f;
        float toRatio = targetCount > 0 ? (float)toValue / targetCount : 0f;
        toRatio = Mathf.Min(toRatio, 1f);

        // 缓存起始宽度，避免每帧重新计算
        float fromWidth = fromRatio * fullBarWidth;
        float toWidth = toRatio * fullBarWidth;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // SmoothStep 缓入缓出
            float smoothT = t * t * (3f - 2f * t);

            // 进度条平滑过渡（宽度）
            if (fillRectTransform != null)
            {
                float currentWidth = Mathf.Lerp(fromWidth, toWidth, smoothT);
                fillRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentWidth);
            }

            // 数字平滑滚动
            int displayNum = Mathf.RoundToInt(Mathf.Lerp(fromValue, toValue, smoothT));
            if (progressText != null)
                progressText.text = $"{displayNum}/{targetCount}";

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 确保最终值精确
        currentCount = toValue;
        SetFillWidth(targetCount > 0 ? (float)currentCount / targetCount : 0f);

        if (progressText != null)
            progressText.text = $"{currentCount}/{targetCount}";

        // 完成后判断是否显示已完成图标
        if (completedIcon != null)
            completedIcon.gameObject.SetActive(currentCount >= targetCount);
    }
}