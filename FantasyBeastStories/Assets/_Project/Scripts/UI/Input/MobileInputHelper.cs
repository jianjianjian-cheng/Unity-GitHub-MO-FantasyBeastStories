using UnityEngine;

namespace UI.Input
{
  /// <summary>
  /// 移动端/PC 统一输入工具类
  /// 自动检测触摸输入，回退到鼠标输入（PC 编辑器 / 非触摸设备）
  /// 所有方法均为静态，即插即用
  /// </summary>
  public static class MobileInputHelper
  {
    /// <summary>
    /// 当前是否为触摸设备（运行时检测）
    /// </summary>
    public static bool IsTouchDevice => UnityEngine.Input.touchSupported;

    /// <summary>
    /// 当前触摸/点击数量
    /// </summary>
    public static int PointerCount
    {
      get
      {
        if (UnityEngine.Input.touchCount > 0)
          return UnityEngine.Input.touchCount;
        // 鼠标也算一个指针
        return UnityEngine.Input.GetMouseButton(0) || UnityEngine.Input.GetMouseButtonDown(0) || UnityEngine.Input.GetMouseButtonUp(0) ? 1 : 0;
      }
    }

    /// <summary>
    /// 获取屏幕位置（触摸位置 / 鼠标位置）
    /// 手机：返回第一根手指的位置
    /// PC：返回鼠标位置
    /// </summary>
    public static Vector2 GetScreenPosition()
    {
      if (UnityEngine.Input.touchCount > 0)
        return UnityEngine.Input.GetTouch(0).position;
      return UnityEngine.Input.mousePosition;
    }

    /// <summary>
    /// 获取屏幕位置（可指定触摸索引）
    /// </summary>
    public static Vector2 GetScreenPosition(int touchIndex)
    {
      if (UnityEngine.Input.touchCount > touchIndex)
        return UnityEngine.Input.GetTouch(touchIndex).position;
      return UnityEngine.Input.mousePosition;
    }

    /// <summary>
    /// 是否有指针按下（触摸开始 / 鼠标左键按下）
    /// </summary>
    public static bool GetPointerDown()
    {
      if (UnityEngine.Input.touchCount > 0)
        return UnityEngine.Input.GetTouch(0).phase == TouchPhase.Began;
      return UnityEngine.Input.GetMouseButtonDown(0);
    }

    /// <summary>
    /// 是否有指针抬起（触摸结束 / 鼠标左键松开）
    /// </summary>
    public static bool GetPointerUp()
    {
      if (UnityEngine.Input.touchCount > 0)
      {
        var touch = UnityEngine.Input.GetTouch(0);
        return touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
      }
      return UnityEngine.Input.GetMouseButtonUp(0);
    }

    /// <summary>
    /// 指针是否正在按住（触摸移动/静止 / 鼠标左键按住）
    /// </summary>
    public static bool GetPointerHeld()
    {
      if (UnityEngine.Input.touchCount > 0)
      {
        var touch = UnityEngine.Input.GetTouch(0);
        return touch.phase == TouchPhase.Moved
            || touch.phase == TouchPhase.Stationary
            || touch.phase == TouchPhase.Began;
      }
      return UnityEngine.Input.GetMouseButton(0);
    }

    /// <summary>
    /// 获取触摸/鼠标 Delta 移动量
    /// 手机：返回当前帧触摸移动量
    /// PC：返回 (0,0)，因为鼠标没有帧 delta，需外部自己计算
    /// </summary>
    public static Vector2 GetDelta()
    {
      if (UnityEngine.Input.touchCount > 0)
        return UnityEngine.Input.GetTouch(0).deltaPosition;
      return Vector2.zero;
    }

    /// <summary>
    /// 是否有触摸/鼠标正在拖拽（手指移动中 / 鼠标按下并移动）
    /// 与 GetPointerHeld 的区别：此方法要求有位移
    /// </summary>
    public static bool IsDragging(float minDragDistance = 2f)
    {
      if (UnityEngine.Input.touchCount > 0)
      {
        var touch = UnityEngine.Input.GetTouch(0);
        return touch.phase == TouchPhase.Moved && touch.deltaPosition.magnitude > minDragDistance;
      }
      // 鼠标模式下拖拽检测由外部通过 delta 计算，此处返回 held 状态
      return UnityEngine.Input.GetMouseButton(0);
    }
  }
}