using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace UI.Framework.Animation
{
    [CreateAssetMenu(menuName = "UI/Animation/Fade Animation")]
    public class UIFadeAnimation : UIAnimationBase
    {
        [Header("淡入淡出设置")]
        [SerializeField] protected float startAlpha = 0f;
        [SerializeField] protected float endAlpha = 1f;

        public override IEnumerator PlayCoroutine(GameObject target)
        {
            Stop();

            CanvasGroup canvasGroup = GetCanvasGroup(target);

            if (canvasGroup == null)
            {
                Debug.LogWarning("UIFadeAnimation: CanvasGroup 为空");
                yield break;
            }

            canvasGroup.alpha = startAlpha;

            _currentTween = canvasGroup.DOFade(endAlpha, duration)
                .SetEase(ease)
                .SetUpdate(true);

            yield return _currentTween.WaitForCompletion();
        }
    }
}
