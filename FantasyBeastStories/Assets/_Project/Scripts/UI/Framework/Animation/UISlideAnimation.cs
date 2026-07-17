using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace UI.Framework.Animation
{
    public enum SlideDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    [CreateAssetMenu(menuName = "UI/Animation/Slide Animation")]
    public class UISlideAnimation : UIAnimationBase
    {
        [Header("滑动设置")]
        [SerializeField] protected SlideDirection direction = SlideDirection.Left;
        [SerializeField] protected float offset = 500f;

        public override async Task PlayAsync(GameObject target)
        {
            Stop();

            RectTransform rect = GetRectTransform(target);
            
            if (rect == null)
            {
                Debug.LogWarning("UISlideAnimation: RectTransform 为空");
                return;
            }

            Vector2 startPos = rect.anchoredPosition;
            Vector2 targetPos = startPos;

            switch (direction)
            {
                case SlideDirection.Left:
                    startPos.x -= offset;
                    break;
                case SlideDirection.Right:
                    startPos.x += offset;
                    break;
                case SlideDirection.Up:
                    startPos.y += offset;
                    break;
                case SlideDirection.Down:
                    startPos.y -= offset;
                    break;
            }

            rect.anchoredPosition = startPos;

            _currentTween = rect.DOAnchorPos(targetPos, duration)
                .SetEase(ease)
                .SetUpdate(true);

            await _currentTween.AsyncWaitForCompletion();
        }
    }
}