using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Presentation.UI.Framework.Animation
{
    [CreateAssetMenu(menuName = "UI/Animation/Fade Animation")]
    public class UIFadeAnimation : UIAnimationBase
    {
        [Header("淡入淡出设置")]
        [SerializeField] protected float startAlpha = 0f;
        [SerializeField] protected float endAlpha = 1f;

        public override async Task PlayAsync(GameObject target)
        {
            Stop();

            CanvasGroup canvasGroup = GetCanvasGroup(target);
            
            if (canvasGroup == null)
            {
                Debug.LogWarning("UIFadeAnimation: CanvasGroup 为空");
                return;
            }

            canvasGroup.alpha = startAlpha;

            _currentTween = canvasGroup.DOFade(endAlpha, duration)
                .SetEase(ease)
                .SetUpdate(true);

            await _currentTween.AsyncWaitForCompletion();
        }
    }
}