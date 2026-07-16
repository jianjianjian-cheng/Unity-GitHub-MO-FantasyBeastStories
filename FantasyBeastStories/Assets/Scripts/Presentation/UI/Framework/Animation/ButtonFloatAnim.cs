using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Presentation.UI.Framework.Animation
{
    /// <summary>
    /// 按钮交互动画：悬浮上浮 + 点击缩放反馈（DOTween）
    /// 挂载到 Button 上即可生效。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ButtonFloatAnim : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("悬浮动画")]
        [SerializeField] private float hoverOffsetY = 8f;
        [SerializeField] private float hoverScale = 1.06f;
        [SerializeField] private float hoverDuration = 0.18f;
        [SerializeField] private Ease hoverEase = Ease.OutCubic;

        [Header("点击反馈")]
        [SerializeField] private float punchScale = 0.92f;
        [SerializeField] private float punchDuration = 0.12f;
        [SerializeField] private Ease punchEase = Ease.OutQuad;

        private RectTransform rectTransform;
        private Vector2 originalAnchoredPos;
        private Vector3 originalScale;
        private Tween hoverTween;
        private Tween punchTween;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            originalAnchoredPos = rectTransform.anchoredPosition;
            originalScale = rectTransform.localScale;

            var btn = GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(PlayClickPunch);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PlayHover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PlayHoverExit();
        }

        private void PlayHover()
        {
            hoverTween?.Kill();
            hoverTween = DOTween.Sequence()
                .Append(rectTransform.DOAnchorPos(originalAnchoredPos + Vector2.up * hoverOffsetY, hoverDuration).SetEase(hoverEase))
                .Join(rectTransform.DOScale(originalScale * hoverScale, hoverDuration).SetEase(hoverEase))
                .SetUpdate(true);
        }

        private void PlayHoverExit()
        {
            hoverTween?.Kill();
            hoverTween = DOTween.Sequence()
                .Append(rectTransform.DOAnchorPos(originalAnchoredPos, hoverDuration).SetEase(hoverEase))
                .Join(rectTransform.DOScale(originalScale, hoverDuration).SetEase(hoverEase))
                .SetUpdate(true);
        }

        private void PlayClickPunch()
        {
            punchTween?.Kill();
            punchTween = rectTransform.DOScale(originalScale * punchScale, punchDuration)
                .SetEase(punchEase)
                .OnComplete(() =>
                {
                    rectTransform.DOScale(originalScale, punchDuration).SetEase(punchEase).SetUpdate(true);
                })
                .SetUpdate(true);
        }

        private void OnDestroy()
        {
            hoverTween?.Kill();
            punchTween?.Kill();
        }
    }
}
