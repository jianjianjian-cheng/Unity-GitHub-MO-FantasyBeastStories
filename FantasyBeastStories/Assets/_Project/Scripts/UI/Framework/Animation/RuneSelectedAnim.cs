using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace UI.Framework.Animation
{
    /// <summary>
    /// 符文「选中」动画：弹性放大 → 停留在比原始略大的尺寸，
    /// 明确标示"此槽位已被选中"
    /// </summary>
    [CreateAssetMenu(menuName = "UI/Animation/Rune/Selected")]
    public class RuneSelectedAnim : UIAnimationBase
    {
        [Header("选中动画参数")]
        [SerializeField] private float peakScale = 1.2f;
        [SerializeField] private float holdScale = 1.08f;       // 选中后保持的尺寸
        [SerializeField] private float scaleUpDuration = 0.2f;
        [SerializeField] private float settleDuration = 0.18f;

        public override IEnumerator PlayCoroutine(GameObject target)
        {
            Stop();

            RectTransform rect = GetRectTransform(target);
            if (rect == null)
            {
                Debug.LogWarning("RuneSelectedAnim: 目标没有 RectTransform");
                yield break;
            }

            Sequence seq = DOTween.Sequence();
            seq.SetUpdate(true);

            // ① 弹性放大到峰值（带明显弹性）
            seq.Append(rect.DOScale(peakScale, scaleUpDuration)
                .SetEase(Ease.OutBack, overshoot: 2f));

            // ② 回落并停留在选中尺寸，比原始大，一眼看出哪个被选中
            seq.Append(rect.DOScale(holdScale, settleDuration)
                .SetEase(Ease.OutQuad));

            _currentTween = seq;
            yield return _currentTween.WaitForCompletion();
        }
    }
}
