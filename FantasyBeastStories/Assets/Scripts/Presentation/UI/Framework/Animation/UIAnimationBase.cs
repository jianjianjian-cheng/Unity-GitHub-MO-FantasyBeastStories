using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Presentation.UI.Framework.Animation
{
    public abstract class UIAnimationBase : ScriptableObject, IUIAnimation
    {
        [Header("动画基础设置")]
        [SerializeField] protected float duration = 0.3f;
        [SerializeField] protected Ease ease = Ease.OutQuad;
        [SerializeField] protected bool useCanvasGroup = true;

        protected Tween _currentTween;

        public abstract Task PlayAsync(GameObject target);

        public virtual void Play(GameObject target)
        {
            PlayAsync(target).ConfigureAwait(false);
        }

        public virtual void Stop()
        {
            if (_currentTween != null && _currentTween.IsPlaying())
            {
                _currentTween.Kill();
            }
        }

        public bool IsPlaying => _currentTween != null && _currentTween.IsPlaying();

        protected CanvasGroup GetCanvasGroup(GameObject target)
        {
            return target.GetComponent<CanvasGroup>();
        }

        protected RectTransform GetRectTransform(GameObject target)
        {
            return target.GetComponent<RectTransform>();
        }
    }
}