using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Presentation.UI.Framework.Animation
{
    [CreateAssetMenu(menuName = "UI/Animation/Fast Fade Animation")]
    public class UIFastFadeAnimation : UIAnimationBase
    {
        [Header("快速淡入设置")]
        [SerializeField] protected float startAlpha = 0f;
        [SerializeField] protected float endAlpha = 1f;

        public override async Task PlayAsync(GameObject target)
        {
            Stop();

            CanvasGroup canvasGroup = GetCanvasGroup(target);
            
            if (canvasGroup == null)
            {
                canvasGroup = target.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = startAlpha;

            _currentTween = canvasGroup.DOFade(endAlpha, 0.2f)
                .SetEase(Ease.Linear)
                .SetUpdate(true);

            await _currentTween.AsyncWaitForCompletion();
        }
    }
}