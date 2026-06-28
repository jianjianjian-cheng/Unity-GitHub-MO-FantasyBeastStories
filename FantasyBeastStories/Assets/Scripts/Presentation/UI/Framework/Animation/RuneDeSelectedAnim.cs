using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Presentation.UI.Framework.Animation
{
    /// <summary>
    /// 符文「取消选中」动画：从选中尺寸平滑缩回原始大小
    /// </summary>
    [CreateAssetMenu(menuName = "UI/Animation/Rune/DeSelected")]
    public class RuneDeSelectedAnim : UIAnimationBase
    {
        [Header("取消选中动画参数")]
        [SerializeField] private Vector3 normalScale = Vector3.one;
        [SerializeField] private float scaleDuration = 0.2f;

        public override async Task PlayAsync(GameObject target)
        {
            Stop();

            RectTransform rect = GetRectTransform(target);
            if (rect == null)
            {
                Debug.LogWarning("RuneDeSelectedAnim: 目标没有 RectTransform");
                return;
            }

            _currentTween = rect.DOScale(normalScale, scaleDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);

            await _currentTween.AsyncWaitForCompletion();
        }
    }
}