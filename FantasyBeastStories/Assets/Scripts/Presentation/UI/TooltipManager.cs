using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Presentation.UI
{
    /// <summary>
    /// 全局提示管理器
    /// </summary>
    public class TooltipManager : MonoBehaviour
    {
        public static TooltipManager Instance { get; private set; }

        [Header("提示框设置")]
        [SerializeField]
        private Vector2 tooltipOffset = new Vector2(15, -15);

        [SerializeField]
        private Vector2 tooltipSize = new Vector2(200, 50);

        [SerializeField]
        private Color backgroundColor = new Color(0, 0, 0, 0.8f);

        [SerializeField]
        private int fontSize = 14;

        private GameObject tooltipPanel;
        private Text tooltipText;
        private RectTransform tooltipRect;
        private Coroutine followCoroutine;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            CreateTooltip();
        }

        void CreateTooltip()
        {
            Canvas canvas = GetOrCreateCanvas();
            if (canvas == null)
                return;

            tooltipPanel = new GameObject("TooltipPanel");
            tooltipPanel.transform.SetParent(canvas.transform, false);

            tooltipRect = tooltipPanel.AddComponent<RectTransform>();
            tooltipRect.sizeDelta = tooltipSize;

            Image background = tooltipPanel.AddComponent<Image>();
            background.color = backgroundColor;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(tooltipPanel.transform, false);

            tooltipText = textObj.AddComponent<Text>();
            tooltipText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            tooltipText.fontSize = fontSize;
            tooltipText.color = Color.white;
            tooltipText.alignment = TextAnchor.MiddleCenter;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(5, 5);
            textRect.offsetMax = new Vector2(-5, -5);

            tooltipPanel.SetActive(false);
        }

        Canvas GetOrCreateCanvas()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("TooltipCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                DontDestroyOnLoad(canvasObj);
            }
            return canvas;
        }

        public void ShowTooltip(string text)
        {
            if (tooltipPanel != null && tooltipText != null)
            {
                tooltipText.text = text;
                tooltipPanel.SetActive(true);

                if (followCoroutine != null)
                    StopCoroutine(followCoroutine);
                followCoroutine = StartCoroutine(FollowMouse());
            }
        }

        public void HideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
                if (followCoroutine != null)
                {
                    StopCoroutine(followCoroutine);
                    followCoroutine = null;
                }
            }
        }

        IEnumerator FollowMouse()
        {
            while (tooltipPanel.activeSelf)
            {
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    tooltipPanel.transform.parent as RectTransform,
                    Input.mousePosition,
                    null,
                    out localPoint
                );
                tooltipRect.anchoredPosition = localPoint + tooltipOffset;

                // 确保提示框不超出屏幕边界
                Vector2 clampedPosition = tooltipRect.anchoredPosition;
                Rect parentRect = (tooltipPanel.transform.parent as RectTransform).rect;

                if (clampedPosition.x + tooltipRect.rect.width > parentRect.width)
                    clampedPosition.x = parentRect.width - tooltipRect.rect.width;
                if (clampedPosition.x < 0)
                    clampedPosition.x = 0;
                if (clampedPosition.y - tooltipRect.rect.height < -parentRect.height)
                    clampedPosition.y = -parentRect.height + tooltipRect.rect.height;
                if (clampedPosition.y > 0)
                    clampedPosition.y = 0;

                tooltipRect.anchoredPosition = clampedPosition;
                yield return null;
            }
        }
    }
}