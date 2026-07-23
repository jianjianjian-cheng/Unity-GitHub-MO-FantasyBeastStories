using Core;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 任务目标方向指示器。
    /// 有任务时始终可见，目标在屏幕内时跟随目标位置，目标在屏幕外时贴边并指向目标方向。
    /// </summary>
    public class DirectionIndicator : MonoBehaviour
    {
        [Header("目标设置")]
        public Vector3 targetPosition;

        [Header("屏幕边缘偏移")]
        public float edgeOffset = 50f;

        private Camera mainCamera;
        private RectTransform indicatorRect;
        private Image indicatorImage;
        private CanvasGroup canvasGroup;
        private RectTransform parentRect; // 父级 RectTransform（用于坐标系转换）

        [SerializeField]
        private string currentName;

        void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("找不到主摄像机！请确保场景中有MainCamera标签的摄像机");
            }

            indicatorRect = GetComponent<RectTransform>();

            // 获取根 Canvas 的 RectTransform（用于屏幕坐标 → UI 坐标转换）
            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null)
                parentRect = rootCanvas.GetComponent<RectTransform>();

            indicatorImage = GetComponent<Image>();

            // 获取或添加 CanvasGroup 组件
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // 初始隐藏
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
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
                Sprite sprite = AssetLoader.LoadAsset<Sprite>("Icons/" + imageName);
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
            // 防御：组件未就绪时不处理
            if (mainCamera == null || canvasGroup == null || indicatorRect == null)
                return;

            // 无任务时隐藏
            if (string.IsNullOrEmpty(currentName))
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                return;
            }

            // ── 有任务且目标在屏幕内 → 隐藏（玩家能直接看到任务区域，不需要箭头指引） ──
            Vector3 screenPos = mainCamera.WorldToScreenPoint(targetPosition);
            bool isBehindCamera = screenPos.z < 0;

            if (isBehindCamera)
            {
                // 目标在摄像机后方：镜像到屏幕另一侧
                screenPos.x = Screen.width - screenPos.x;
                screenPos.y = Screen.height - screenPos.y;
            }

            bool isTargetOnScreen = !isBehindCamera && IsOnScreen(screenPos);

            if (isTargetOnScreen)
            {
                // 任务区域在屏幕可视范围内 → 隐藏指示器
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                return;
            }

            // ── 目标在屏幕外 → 显示指示器，贴边并指向目标方向 ──
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            // 计算贴边后的屏幕位置
            Vector2 clampedScreenPos = new Vector2(
                Mathf.Clamp(screenPos.x, edgeOffset, Screen.width - edgeOffset),
                Mathf.Clamp(screenPos.y, edgeOffset, Screen.height - edgeOffset)
            );

            // 将屏幕坐标转换到父 Canvas 的本地坐标
            Vector2 localPos;
            if (parentRect != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, clampedScreenPos, null, out localPos);
            }
            else
            {
                localPos = clampedScreenPos;
            }

            // 目标在屏幕外/后方：指示器贴边，箭头指向目标方向
            indicatorRect.anchoredPosition = localPos;

            Vector2 dirToTarget = new Vector2(
                screenPos.x - Screen.width * 0.5f,
                screenPos.y - Screen.height * 0.5f
            );
            float angle = Mathf.Atan2(dirToTarget.y, dirToTarget.x) * Mathf.Rad2Deg;
            indicatorRect.rotation = Quaternion.Euler(0, 0, angle);
        }

        private bool IsOnScreen(Vector3 screenPos)
        {
            return screenPos.x >= edgeOffset
                && screenPos.x <= Screen.width - edgeOffset
                && screenPos.y >= edgeOffset
                && screenPos.y <= Screen.height - edgeOffset;
        }
    }
}