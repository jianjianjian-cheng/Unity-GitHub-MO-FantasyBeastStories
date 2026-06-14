using UnityEngine;
using UnityEngine.UI;

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

    public void SetTargetAndImage(Vector3 position, string imageName)
    {
        this.targetPosition = position;

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
        }

        if (isOnScreen && !isBehindCamera)
        {
            // 在屏幕内：隐藏指示器（但不关闭GameObject）
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            // 不在屏幕内：显示指示器
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            Vector2 clampedScreenPos = ClampToScreenEdge(screenPos);
            indicatorRect.position = clampedScreenPos;
            RotateTowardsTarget(clampedScreenPos, screenPos);
        }
    }

    bool IsOnScreen(Vector3 screenPos)
    {
        return screenPos.x > edgeOffset
            && screenPos.x < Screen.width - edgeOffset
            && screenPos.y > edgeOffset
            && screenPos.y < Screen.height - edgeOffset;
    }

    Vector2 ClampToScreenEdge(Vector3 screenPos)
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 direction = new Vector2(screenPos.x - screenCenter.x, screenPos.y - screenCenter.y);

        if (direction.magnitude < 0.01f)
            direction = Vector2.up;

        float halfWidth = Screen.width / 2f - edgeOffset;
        float halfHeight = Screen.height / 2f - edgeOffset;
        float scaleX = halfWidth / Mathf.Abs(direction.x);
        float scaleY = halfHeight / Mathf.Abs(direction.y);
        float scale = Mathf.Min(scaleX, scaleY);

        return screenCenter + direction * scale;
    }

    void RotateTowardsTarget(Vector2 clampedPos, Vector3 actualTargetPos)
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 direction = new Vector2(
            actualTargetPos.x - screenCenter.x,
            actualTargetPos.y - screenCenter.y
        );

        if (direction.magnitude < 0.01f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        indicatorRect.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }
}
