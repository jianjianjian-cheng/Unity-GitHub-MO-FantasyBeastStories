using UnityEngine;
using UnityEngine.Rendering;

namespace UI
{
    /// <summary>
    /// 挂载到不需要雾效和动态光照的 Camera 上（如小地图相机），
    /// 在 URP 渲染该相机前后临时关闭雾效与所有 Light 组件。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class DisableFogForCamera : MonoBehaviour
    {
        private Camera _camera;
        // 缓存渲染期间被临时关闭的 Light，渲染结束后恢复
        private System.Collections.Generic.List<Light> _disabledLights = new();

        void OnEnable()
        {
            _camera = GetComponent<Camera>();
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            RenderPipelineManager.endCameraRendering += OnEndCamera;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            RenderPipelineManager.endCameraRendering -= OnEndCamera;
        }

        void OnBeginCamera(ScriptableRenderContext ctx, Camera cam)
        {
            if (cam != _camera) return;

            RenderSettings.fog = false;

            _disabledLights.Clear();
            foreach (var light in LightEnumerator.Cache)
            {
                if (light != null && light.enabled && light.gameObject.activeInHierarchy)
                {
                    light.enabled = false;
                    _disabledLights.Add(light);
                }
            }
        }

        void OnEndCamera(ScriptableRenderContext ctx, Camera cam)
        {
            if (cam != _camera) return;

            RenderSettings.fog = true;

            foreach (var light in _disabledLights)
            {
                if (light != null)
                    light.enabled = true;
            }
            _disabledLights.Clear();
        }

        /// <summary>
        /// 每帧更新的场景 Light 缓存，避免渲染回调中 FindObjectsOfType。
        /// </summary>
        private static class LightEnumerator
        {
            private static readonly System.Collections.Generic.List<Light> _lights = new();
            private static float _lastRefreshTime = -1f;

            public static System.Collections.Generic.List<Light> Cache
            {
                get
                {
                    if (Time.unscaledTime != _lastRefreshTime)
                    {
                        _lastRefreshTime = Time.unscaledTime;
                        _lights.Clear();
                        _lights.AddRange(Object.FindObjectsByType<Light>(FindObjectsSortMode.None));
                    }
                    return _lights;
                }
            }
        }
    }
}
