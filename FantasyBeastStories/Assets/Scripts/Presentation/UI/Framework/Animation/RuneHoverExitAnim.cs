using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Presentation.UI.Framework.Animation
{
    /// <summary>
    /// 符文「悬浮离开」动画：快速回到原始大小
    /// </summary>
    [CreateAssetMenu(menuName = "UI/Animation/Rune/HoverExit")]
    public class RuneHoverExitAnim : UIAnimationBase
    {
        [Header("悬浮离开参数")]
        [SerializeField] private Vector3 normalScale = Vector3.one;
        [SerializeField] private float durationOut = 0.1f;

        public override async Task PlayAsync(GameObject target)
        {
            Stop();

            RectTransform rect = GetRectTransform(target);
            if (rect == null)
            {
                Debug.LogWarning("RuneHoverExitAnim: 目标没有 RectTransform");
                return;
            }

            _currentTween = rect.DOScale(normalScale, durationOut)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);

            await _currentTween.AsyncWaitForCompletion();
        }
    }
}