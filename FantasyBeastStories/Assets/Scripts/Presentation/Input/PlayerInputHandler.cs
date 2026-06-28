using UnityEngine;

namespace Presentation.PlayerInput
{
    /// <summary>
    /// 输入处理器（纯 C# 实例类）
    /// 职责：仅读取 Unity 输入系统的原始数据，不做任何业务逻辑判断
    /// 由所在模块（如 PlayerController）驱动 Update()
    /// </summary>
    public class PlayerInputHandler
    {
        // ==================== 移动轴 ====================
        public float Horizontal { get; private set; }
        public float Vertical { get; private set; }

        // ==================== 鼠标位置 ====================
        public Vector3 MousePosition { get; private set; }

        // ==================== 键盘按键（帧事件） ====================
        public bool IsEscapePressed { get; private set; }
        public bool IsReturnPressed { get; private set; }
        public bool IsTabPressed { get; private set; }
        public bool IsTabReleased { get; private set; }

        // ==================== 鼠标按键（帧事件） ====================
        public bool IsMouseButtonDown { get; private set; }
        public bool IsMouseButtonUp { get; private set; }
        public bool IsMouseHeld { get; private set; }

        /// <summary>
        /// 每帧更新所有输入状态，由持有者（如 PlayerController）调用
        /// </summary>
        public void Update()
        {
            Horizontal = UnityEngine.Input.GetAxis("Horizontal");
            Vertical = UnityEngine.Input.GetAxis("Vertical");

            MousePosition = UnityEngine.Input.mousePosition;

            IsEscapePressed = UnityEngine.Input.GetKeyDown(KeyCode.Escape);
            IsReturnPressed = UnityEngine.Input.GetKeyDown(KeyCode.Return);
            IsTabPressed = UnityEngine.Input.GetKeyDown(KeyCode.Tab);
            IsTabReleased = UnityEngine.Input.GetKeyUp(KeyCode.Tab);

            IsMouseButtonDown = UnityEngine.Input.GetMouseButtonDown(0);
            IsMouseButtonUp = UnityEngine.Input.GetMouseButtonUp(0);
            IsMouseHeld = UnityEngine.Input.GetMouseButton(0);
        }
    }
}