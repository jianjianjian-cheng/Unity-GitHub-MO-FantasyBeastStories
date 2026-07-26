using UnityEngine;

namespace UI.Input
{
    /// <summary>
    /// 输入更新驱动器
    /// 在游戏启动时自动创建，每帧驱动 PlayerInputHandler.Instance.Update() 采集全局输入
    /// </summary>
    public class InputUpdater : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var go = new GameObject("[InputUpdater]");
            DontDestroyOnLoad(go);
            go.AddComponent<InputUpdater>();
        }

        private void Update()
        {
            // PlayerInputHandler 由 PlayerController 中的实例自行驱动 Update
        }
    }
}