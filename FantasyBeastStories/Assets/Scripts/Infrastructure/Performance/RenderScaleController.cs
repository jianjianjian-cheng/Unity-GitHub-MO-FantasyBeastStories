using UnityEngine;

namespace Infrastructure.Performance
{
    public class RenderScaleController : MonoBehaviour
    {
        [Header("渲染分辨率比例")]
        [Range(0.25f, 1f)]
        [SerializeField] private float renderScale = 0.75f;

        private int _originalWidth;
        private int _originalHeight;
        private UnityEngine.Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            if (_camera == null)
                _camera = UnityEngine.Camera.main;

            if (_camera == null)
            {
                Debug.LogWarning("[RenderScaleController] 未找到摄像机");
                enabled = false;
                return;
            }

            _originalWidth = Screen.width;
            _originalHeight = Screen.height;
            ApplyRenderScale();

            Debug.Log($"[RenderScaleController] 初始化完成，渲染比例: {renderScale:P0}，实际分辨率: {Screen.width}x{Screen.height}");
        }

        private void ApplyRenderScale()
        {
            int targetWidth = Mathf.RoundToInt(_originalWidth * renderScale);
            int targetHeight = Mathf.RoundToInt(_originalHeight * renderScale);

            Screen.SetResolution(targetWidth, targetHeight, true);
        }
    }
}