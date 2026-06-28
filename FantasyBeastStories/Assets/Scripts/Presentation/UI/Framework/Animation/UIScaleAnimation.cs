using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Presentation.UI.Framework.Animation
{
    [CreateAssetMenu(menuName = "UI/Animation/Scale Animation")]
    public class UIScaleAnimation : UIAnimationBase
    {
        [Header("缩放设置")]
        [SerializeField] protected Vector3 startScale = Vector3.zero;
        [SerializeField] protected Vector3 endScale = Vector3.one;
        [SerializeField] protected bool centerPivot = true;

        public override async Task PlayAsync(GameObject target)
        {
            Stop();

            RectTransform rect = GetRectTransform(target);
            
            if (rect == null)
            {
                Debug.LogWarning("UIScaleAnimation: RectTransform 为空");
                return;
            }

            if (centerPivot)
            {
                rect.pivot = new Vector2(0.5f, 0.5f);
            }

            rect.localScale = startScale;

            _currentTween = rect.DOScale(endScale, duration)
                .SetEase(ease)
                .SetUpdate(true);

            await _currentTween.AsyncWaitForCompletion();
        }
    }
}