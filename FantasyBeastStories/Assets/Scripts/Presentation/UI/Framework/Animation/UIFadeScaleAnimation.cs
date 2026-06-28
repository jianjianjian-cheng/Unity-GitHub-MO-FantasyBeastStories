using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Presentation.UI.Framework.Animation
{
    [CreateAssetMenu(menuName = "UI/Animation/Fade Scale Animation")]
    public class UIFadeScaleAnimation : UIAnimationBase
    {
        [Header("淡入缩放设置")]
        [SerializeField] protected float startAlpha = 0f;
        [SerializeField] protected float endAlpha = 1f;
        [SerializeField] protected Vector3 startScale = new Vector3(0.9f, 0.9f, 1f);
        [SerializeField] protected Vector3 peakScale = new Vector3(1.05f, 1.05f, 1f);
        [SerializeField] protected Vector3 endScale = Vector3.one;
        [SerializeField] protected float fadeDuration = 0.2f;
        [SerializeField] protected float scaleDuration = 0.3f;

        public override async Task PlayAsync(GameObject target)
        {
            Stop();

            CanvasGroup canvasGroup = GetCanvasGroup(target);
            
            if (canvasGroup == null)
            {
                canvasGroup = target.AddComponent<CanvasGroup>();
            }

            RectTransform rect = GetRectTransform(target);
            
            if (rect == null)
            {
                rect = target.GetComponent<RectTransform>();
            }

            canvasGroup.alpha = startAlpha;
            rect.localScale = startScale;

            Sequence sequence = DOTween.Sequence();

            sequence.Append(canvasGroup.DOFade(endAlpha, fadeDuration)
                .SetEase(Ease.Linear)
                .SetUpdate(true));

            sequence.Join(rect.DOScale(peakScale, scaleDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true));

            sequence.Append(rect.DOScale(endScale, scaleDuration * 0.5f)
                .SetEase(Ease.InQuad)
                .SetUpdate(true));

            _currentTween = sequence;

            await sequence.AsyncWaitForCompletion();
        }
    }
}