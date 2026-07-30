using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace UI.Framework.Animation
{
    /// <summary>
    /// 符文「悬浮进入」动画：略微放大 + 微亮，提示可点击
    /// </summary>
    [CreateAssetMenu(menuName = "UI/Animation/Rune/HoverEnter")]
    public class RuneHoverEnterAnim : UIAnimationBase
    {
        [Header("悬浮进入参数")]
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float durationIn = 0.12f;

        public override IEnumerator PlayCoroutine(GameObject target)
        {
            Stop();

            RectTransform rect = GetRectTransform(target);
            if (rect == null)
            {
                Debug.LogWarning("RuneHoverEnterAnim: 目标没有 RectTransform");
                yield break;
            }

            // 悬浮时只放大，不影响透明度（避免和选中动画冲突）
            _currentTween = rect.DOScale(hoverScale, durationIn)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);

            yield return _currentTween.WaitForCompletion();
        }
    }
}
