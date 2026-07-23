using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace UI.Framework.Animation
{
    public abstract class UIAnimationBase : ScriptableObject, IUIAnimation
    {
        [Header("动画基础设置")]
        [SerializeField] protected float duration = 0.3f;
        [SerializeField] protected Ease ease = Ease.OutQuad;
        [SerializeField] protected bool useCanvasGroup = true;

        protected Tween _currentTween;

        public abstract IEnumerator PlayCoroutine(GameObject target);

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
