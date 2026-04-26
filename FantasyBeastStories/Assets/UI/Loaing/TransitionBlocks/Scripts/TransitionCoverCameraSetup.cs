using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TransitionCoverCameraSetup : MonoBehaviour
{
    void Start()
    {
        var transitioner = FindObjectOfType<Transitioner>();
        if (transitioner != null && transitioner._transitionCamera == null)
        {
            // 创建一个新相机
            GameObject camGo = new GameObject("TransitionCoverCamera");
            Camera cam = camGo.AddComponent<Camera>();

            // 不渲染场景，只渲染转场方块
            cam.cullingMask = 0;

            // 用代码设置渲染顺序——让它在所有相机之后渲染
            // 直接设最高优先级
            cam.depth = 999;

            // URP额外设置
            var urpData = cam.GetUniversalAdditionalCameraData();
            if (urpData == null)
                urpData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            urpData.renderType = CameraRenderType.Overlay;

            // 把主相机的Stack加上这个Overlay相机
            var mainCamData = Camera.main.GetUniversalAdditionalCameraData();
            if (mainCamData != null)
            {
                mainCamData.cameraStack.Add(cam);
            }

            // 把这个相机赋给Transitioner
            transitioner._transitionCamera = cam;

            Debug.Log("过渡覆盖相机已自动创建并配置完成！");
        }
    }
}