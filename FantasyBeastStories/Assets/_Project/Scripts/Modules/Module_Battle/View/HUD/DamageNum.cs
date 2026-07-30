using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Core;

namespace UI
{
  public class DamageNum : MonoBehaviour
  {
    [Header("基础设置")]
    [SerializeField] private Text damageText;
    [SerializeField] private float floatDistance = 1.5f;    // 向上飘起高度
    [SerializeField] private float duration = 1.5f;         // 总时长

    [Header("缩放设置")]
    [SerializeField] private Vector3 startScale = new Vector3(0.5f, 0.5f, 0.5f);  // 起始大小
    [SerializeField] private Vector3 peakScale = new Vector3(1.2f, 1.2f, 1.2f);    // 最大大小（峰值）
    [SerializeField] private float scaleUpDuration = 0.2f;    // 从小变大的时长
    [SerializeField] private float scaleHoldDuration = 0.3f;  // 保持在最大大小的时长
    [SerializeField] private Ease scaleUpEase = Ease.OutBack; // 放大缓动曲线

    [Header("淡出设置")]
    [SerializeField] private float fadeOutDelay = 0.6f;      // 延迟多久后开始淡出（从动画开始计算）
    [SerializeField] private float fadeOutDuration = 0.4f;   // 淡出持续时长
    [SerializeField] private Ease fadeOutEase = Ease.InQuad; // 淡出缓动曲线

    [Header("暴击设置")]
    [SerializeField] private Color criticalColor = new Color(1f, 0.2f, 0f); // 暴击颜色
    [SerializeField] private int criticalFontSize = 40;
    [SerializeField] private Vector3 criticalPeakScale = new Vector3(1.5f, 1.5f, 1.5f); // 暴击时的最大大小
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private int normalFontSize = 28;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Camera mainCamera;

    // 保存对象池创建时的初始缩放
    private Vector3 initialScale;
    // 标记是否正在播放动画（防止重复播放）
    private bool isPlaying = false;

    private void Awake()
    {
      rectTransform = GetComponent<RectTransform>();
      canvasGroup = GetComponent<CanvasGroup>();
      if (canvasGroup == null)
        canvasGroup = gameObject.AddComponent<CanvasGroup>();

      mainCamera = Camera.main;

      // 保存初始缩放（预制体的原始缩放）
      initialScale = rectTransform.localScale;
    }

    private void Update()
    {
      // 始终面向摄像机
      if (mainCamera != null)
      {
        Vector3 direction = transform.position - mainCamera.transform.position;
        transform.rotation = Quaternion.LookRotation(direction);
      }
    }

    private void OnEnable()
    {
      // 每次从对象池取出时重置状态
      ResetState();
    }

    private void OnDisable()
    {
      // 禁用时清理所有动画
      KillAllTweens();
    }

    /// <summary>
    /// 外部调用：播放伤害数字动画
    /// </summary>
    /// <param name="damage">伤害数值</param>
    /// <param name="worldPos">世界坐标起始位置（角色头顶）</param>
    /// <param name="isCritical">是否暴击（影响颜色/大小）</param>
    public void Play(float damage, Vector3 worldPos, bool isCritical = false)
    {
      // 如果正在播放动画，先停止
      if (isPlaying)
      {
        Debug.LogWarning($"DamageNum is already playing, resetting: {gameObject.name}");
        KillAllTweens();
      }

      // 确保状态已重置
      ResetState();

      // 1. 设置文本和初始位置
      damageText.text = damage.ToString();
      transform.position = worldPos;

      // 2. 设置暴击或普通样式
      if (isCritical)
      {
        damageText.color = criticalColor;
        damageText.fontSize = criticalFontSize;
      }
      else
      {
        damageText.color = normalColor;
        damageText.fontSize = normalFontSize;
      }

      // 3. 播放动画序列
      isPlaying = true;
      PlayAnimationSequence(isCritical);
    }

    /// <summary>
    /// 播放动画序列：弹出到最大 -> 保持 -> 淡出 -> 恢复大小
    /// </summary>
    private void PlayAnimationSequence(bool isCritical)
    {
      // 设置初始缩放（起始大小）
      rectTransform.localScale = startScale;

      // 设置初始透明度（完全不透明）
      canvasGroup.alpha = 1f;

      // 选择最大缩放值（暴击用更大的）
      Vector3 currentPeakScale = isCritical ? criticalPeakScale : peakScale;

      // 创建序列动画
      Sequence sequence = DOTween.Sequence();

      // 动画1: 从起始大小缩放到最大大小
      sequence.Append(
          rectTransform.DOScale(currentPeakScale, scaleUpDuration)
              .SetEase(scaleUpEase)
      );

      // 动画2: 保持在最大大小
      sequence.AppendInterval(scaleHoldDuration);

      // 动画3: 在保持最大大小的同时开始淡出
      sequence.Insert(
          scaleUpDuration + scaleHoldDuration * 0.5f,  // 在保持阶段的后半段开始淡出
          canvasGroup.DOFade(0f, fadeOutDuration)
              .SetEase(fadeOutEase)
      );

      // 动画4: 向上飘起（全程进行）
      sequence.Insert(
          0,  // 从动画开始就向上飘
          rectTransform.DOMoveY(
              transform.position.y + floatDistance,
              duration
          ).SetEase(Ease.OutQuad)
      );

      // 动画完成后的回调
      sequence.OnComplete(() =>
      {
        // 淡出完成后，恢复到初始大小（已经透明了，所以看不到变化）
        rectTransform.localScale = initialScale;
        OnAnimationComplete();
      });

      // 如果动画被中断也要清理
      sequence.OnKill(() =>
      {
        if (isPlaying)
        {
          // 恢复到初始大小
          rectTransform.localScale = initialScale;
          OnAnimationComplete();
        }
      });
    }

    /// <summary>
    /// 动画完成时的处理
    /// </summary>
    private void OnAnimationComplete()
    {
      if (!isPlaying) return;

      isPlaying = false;

      // 确保缩放恢复到初始值
      rectTransform.localScale = initialScale;

      // 回收到对象池
      ServiceLocator.Get<ObjectPoolManager>()?.ReturnToPool(ObjectPoolConst.DamageNumPool, gameObject);
    }

    /// <summary>
    /// 重置所有状态到初始值
    /// </summary>
    private void ResetState()
    {
      isPlaying = false;

      // 恢复到初始缩放
      if (initialScale != Vector3.zero)
      {
        rectTransform.localScale = initialScale;
      }

      // 重置透明度
      canvasGroup.alpha = 1f;

      // 清空文本
      damageText.text = "";

      // 重置颜色和字号为默认值
      damageText.color = normalColor;
      damageText.fontSize = normalFontSize;
    }

    /// <summary>
    /// 强制停止所有动画（用于中断或紧急停止）
    /// </summary>
    public void StopImmediately()
    {
      isPlaying = false;
      KillAllTweens();
      ResetState();

      // 立即回收到对象池
      ServiceLocator.Get<ObjectPoolManager>()?.ReturnToPool(ObjectPoolConst.DamageNumPool, gameObject);
    }

    /// <summary>
    /// 杀死所有 DOTween 动画
    /// </summary>
    private void KillAllTweens()
    {
      rectTransform.DOKill();
      canvasGroup.DOKill();

      // 额外保险：杀掉这个对象上的所有 Tweens
      DOTween.Kill(rectTransform);
      DOTween.Kill(canvasGroup);
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器可视化调试
    /// </summary>
    private void OnDrawGizmos()
    {
      if (isPlaying && damageText != null)
      {
        // 显示伤害数字的作用范围
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 0.2f);

        // 显示浮动终点
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(
            transform.position + Vector3.up * floatDistance,
            0.15f
        );

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.5f,
            $"Damage: {damageText.text}"
        );
      }
    }
#endif
  }
}