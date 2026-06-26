using UnityEngine;
using UnityEngine.UI;

namespace Presentation.UI
{
    public class DirectionIndicator : MonoBehaviour
    {
        [Header("目标设置")]
        public Vector3 targetPosition;

        [Header("屏幕边缘偏移")]
        public float edgeOffset = 50f;

        private Camera mainCamera;
        private RectTransform indicatorRect;
        private Image indicatorImage;
        private CanvasGroup canvasGroup; // 添加CanvasGroup控制可见性

        [SerializeField]
        private string currentName;

        void Start()
        {
            mainCamera = Camera.main;
            currentName = null;
            if (mainCamera == null)
            {
                Debug.LogError("找不到主摄像机！请确保场景中有MainCamera标签的摄像机");
            }

            indicatorRect = GetComponent<RectTransform>();
            indicatorImage = GetComponent<Image>();

            // 获取或添加CanvasGroup组件
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void SetTargetName(string name)
        {
            currentName = name;
        }

        public void SetTargetAndImage(Vector3 position, string imageName)
        {
            this.targetPosition = position;
            currentName = imageName;
            if (indicatorImage == null)
                indicatorImage = GetComponent<Image>();

            if (indicatorImage != null)
            {
                Sprite sprite = Resources.Load<Sprite>("Icons/" + imageName);
                if (sprite != null)
                {
                    indicatorImage.sprite = sprite;
                }
            }
        }

        public void SetTargetPosition(Vector3 position)
        {
            this.targetPosition = position;
        }

        void Update()
        {
            if (mainCamera == null)
                return;

            Vector3 screenPos = mainCamera.WorldToScreenPoint(targetPosition);
            bool isBehindCamera = screenPos.z < 0;

            if (isBehindCamera)
            {
                screenPos.x = Screen.width - screenPos.x;
                screenPos.y = Screen.height - screenPos.y;
            }

            bool isOnScreen = IsOnScreen(screenPos);
            if (currentName == null)
            {
                // 当前无任务：隐藏指示器（但不关闭GameObject）
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                return;
            }
            else
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (isOnScreen && !isBehindCamera)
            {
                // 在屏幕内：隐藏指示器（但不关闭GameObject）
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        bool IsOnScreen(Vector3 screenPos)
        {
            return screenPos.x >= edgeOffset
                && screenPos.x <= Screen.width - edgeOffset
                && screenPos.y >= edgeOffset
                && screenPos.y <= Screen.height - edgeOffset;
        }
    }
}