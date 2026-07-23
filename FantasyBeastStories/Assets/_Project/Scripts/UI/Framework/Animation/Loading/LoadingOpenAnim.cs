using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace UI.Framework.Animation
{
    /// <summary>
    /// Loading 面板打开动画：透明 → 不透明（淡入）
    /// </summary>
    [CreateAssetMenu(menuName = "UI/Animation/Loading Open Anim")]
    public class LoadingOpenAnim : UIAnimationBase
    {
        [Header("淡入设置")]
        [SerializeField] private float startAlpha = 0f;
        [SerializeField] private float endAlpha = 1f;

        public override IEnumerator PlayCoroutine(GameObject target)
        {
            Stop();

            CanvasGroup canvasGroup = GetCanvasGroup(target);

            if (canvasGroup == null)
            {
                Debug.LogWarning("LoadingOpenAnim: 目标缺少 CanvasGroup 组件");
                yield break;
            }

            // 确保从透明开始
            canvasGroup.alpha = startAlpha;
            canvasGroup.blocksRaycasts = true;

            _currentTween = canvasGroup.DOFade(endAlpha, duration)
                .SetEase(ease)
                .SetUpdate(true);

            yield return _currentTween.WaitForCompletion();
        }
    }
}
