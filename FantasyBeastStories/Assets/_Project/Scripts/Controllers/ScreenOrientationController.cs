using UnityEngine;

namespace Controllers
{
    public class ScreenOrientationController : MonoBehaviour
    {
        private void Awake()
        {
            ForceLandscape();
        }

        private void OnEnable()
        {
            ForceLandscape();
        }

        private void ForceLandscape()
        {
#if UNITY_ANDROID || UNITY_IOS
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = false;
#endif

            Debug.Log("[ScreenOrientationController] 已强制锁定为横屏模式");
        }
    }
}