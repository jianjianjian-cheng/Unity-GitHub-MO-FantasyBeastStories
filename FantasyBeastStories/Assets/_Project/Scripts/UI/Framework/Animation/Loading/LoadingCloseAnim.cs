using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace UI.Framework.Animation
{
    /// <summary>
    /// Loading 面板关闭动画：不透明 → 透明（淡出）
    /// </summary>
    [CreateAssetMenu(menuName = "UI/Animation/Loading Close Anim")]
    public class LoadingCloseAnim : UIAnimationBase
    {
        [Header("动画时长")]
        [SerializeField] private float animDuration = 1f;

        [Header("淡出设置")]
        [SerializeField] private float startAlpha = 1f;
        [SerializeField] private float endAlpha = 0f;

        public override IEnumerator PlayCoroutine(GameObject target)
        {
            Stop();

            CanvasGroup canvasGroup = GetCanvasGroup(target);

            if (canvasGroup == null)
            {
                Debug.LogWarning("LoadingCloseAnim: 目标缺少 CanvasGroup 组件");
                yield break;
            }

            // 从不透明开始淡出
            canvasGroup.alpha = startAlpha;
            canvasGroup.blocksRaycasts = false;

            _currentTween = canvasGroup.DOFade(endAlpha, animDuration)
                .SetEase(ease)
                .SetUpdate(true);

            yield return _currentTween.WaitForCompletion();
        }
    }
}
