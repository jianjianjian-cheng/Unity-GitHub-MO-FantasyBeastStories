using UnityEngine;
using UnityEngine.EventSystems;

namespace Presentation.PlayerInput
{
    /// <summary>
    /// 虚拟摇杆控制器
    /// 挂载到摇杆 Panel 上，自动处理拖拽输入
    /// 输出标准化方向值（-1 ~ 1），供 PlayerInputHandler 读取
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("摇杆组件")]
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform knob;

        [Header("摇杆参数")]
        [SerializeField] private float deadZone = 0.1f;

        /// <summary>
        /// 当前摇杆输出方向（标准化值 -1 ~ 1）
        /// </summary>
        public Vector2 Direction { get; private set; }

        /// <summary>
        /// 摇杆是否正在被使用
        /// </summary>
        public bool IsActive { get; private set; }

        private float _maxRadius;

        private void Awake()
        {
            if (background == null)
                background = GetComponent<RectTransform>();

            if (knob == null)
            {
                // 尝试从子对象中查找名为 "Knob" 的 RectTransform
                Transform knobTransform = transform.Find("Knob");
                if (knobTransform != null)
                    knob = knobTransform.GetComponent<RectTransform>();
                else
                    knob = transform.GetChild(0)?.GetComponent<RectTransform>();
            }

            // 计算摇杆最大半径（使用 rect 获取实际渲染尺寸，不受 anchor 影响）
            _maxRadius = background.rect.width / 2f;
            Direction = Vector2.zero;

            // 自动注册为当前激活摇杆
            PlayerInputHandler.ActiveJoystick = this;

            Debug.Log($"[VirtualJoystick] 初始化完成，最大半径: {_maxRadius}，背景尺寸: {background.rect.size}");

            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                Debug.LogWarning("[VirtualJoystick] 场景中未找到 EventSystem，UI 事件将无法工作！");
        }

        private void OnDestroy()
        {
            // 销毁时取消注册
            if (PlayerInputHandler.ActiveJoystick == this)
            {
                PlayerInputHandler.ActiveJoystick = null;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsActive = true;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    background,
                    eventData.position,
                    eventData.pressEventCamera,
                    out localPoint))
            {
                // localPoint 在背景 RectTransform 的本地空间：
                // 中心 (0,0)，向右 +X，向上 +Y，所以 localPoint 本身就是偏移量
                float magnitude = localPoint.magnitude;

                // 限制半径
                Vector2 offset = localPoint;
                if (magnitude > _maxRadius)
                {
                    offset = localPoint.normalized * _maxRadius;
                    magnitude = _maxRadius;
                }

                // 移动摇杆圆点
                knob.anchoredPosition = offset;

                // 计算标准化方向（-1 ~ 1）
                float normalizedMagnitude = magnitude / _maxRadius;
                Direction = offset.normalized * Mathf.Clamp01(normalizedMagnitude);

                // 应用死区
                if (Direction.magnitude < deadZone)
                {
                    Direction = Vector2.zero;
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsActive = false;
            Direction = Vector2.zero;
            knob.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 重置摇杆状态
        /// </summary>
        public void ResetJoystick()
        {
            IsActive = false;
            Direction = Vector2.zero;
            knob.anchoredPosition = Vector2.zero;
        }

        private void OnDisable()
        {
            ResetJoystick();
        }
    }
}