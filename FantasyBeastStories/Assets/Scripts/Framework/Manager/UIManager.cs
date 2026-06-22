using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

namespace Framework
{
  /// <summary>
  /// UI面板接口
  /// </summary>
  public interface IUIPanel
  {
    string PanelId { get; }
    bool IsOpen { get; }
    void Open();
    void Close();
  }

  /// <summary>
  /// UI管理器 - 集中管理所有UI面板
  /// </summary>
  public class UIManager : Core.MonoSingleton<UIManager>
  {
    private readonly Dictionary<string, GameObject> _panels = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, Tween> _openTweens = new Dictionary<string, Tween>();

    [SerializeField] private EventSystem _eventSystem;
    [SerializeField] private Canvas _mainCanvas;

    public EventSystem EventSystem => _eventSystem;
    public Canvas MainCanvas => _mainCanvas;

    protected override void Awake()
    {
      base.Awake();

      if (_eventSystem == null)
      {
        _eventSystem = FindObjectOfType<EventSystem>();
      }

      if (_mainCanvas == null)
      {
        _mainCanvas = FindObjectOfType<Canvas>();
      }
    }

    /// <summary>
    /// 注册面板
    /// </summary>
    public void RegisterPanel(string panelId, GameObject panel)
    {
      if (_panels.ContainsKey(panelId))
      {
        Debug.LogWarning($"[UIManager] Panel '{panelId}' already registered!");
        return;
      }

      _panels[panelId] = panel;
      panel.SetActive(false);
    }

    /// <summary>
    /// 获取面板
    /// </summary>
    public GameObject GetPanel(string panelId)
    {
      if (_panels.TryGetValue(panelId, out var panel))
      {
        return panel;
      }

      Debug.LogWarning($"[UIManager] Panel '{panelId}' not found!");
      return null;
    }

    /// <summary>
    /// 打开面板（基础版本）
    /// </summary>
    public void OpenPanel(string panelId)
    {
      if (!_panels.TryGetValue(panelId, out var panel))
      {
        Debug.LogWarning($"[UIManager] Panel '{panelId}' not found!");
        return;
      }

      if (panel.activeSelf) return;

      panel.SetActive(true);
      Core.Event.EventManager.Instance.Emit(new PanelOpenedEvent(panelId));
    }

    /// <summary>
    /// 关闭面板（基础版本）
    /// </summary>
    public void ClosePanel(string panelId)
    {
      if (!_panels.TryGetValue(panelId, out var panel))
      {
        Debug.LogWarning($"[UIManager] Panel '{panelId}' not found!");
        return;
      }

      if (!panel.activeSelf) return;

      panel.SetActive(false);
      Core.Event.EventManager.Instance.Emit(new PanelClosedEvent(panelId));
    }

    /// <summary>
    /// 带动画打开面板
    /// </summary>
    public void OpenPanelWithAnimation(string panelId, Vector2 startPos, Vector2 endPos, float duration = 0.3f, Ease ease = Ease.OutBack)
    {
      if (!_panels.TryGetValue(panelId, out var panel))
      {
        Debug.LogWarning($"[UIManager] Panel '{panelId}' not found!");
        return;
      }

      // 停止之前的动画
      if (_openTweens.TryGetValue(panelId, out var oldTween))
      {
        oldTween.Kill();
      }

      var rect = panel.GetComponent<RectTransform>();
      if (rect == null)
      {
        OpenPanel(panelId);
        return;
      }

      panel.SetActive(true);
      rect.anchoredPosition = startPos;

      var tween = rect.DOAnchorPos(endPos, duration).SetEase(ease);
      _openTweens[panelId] = tween;

      tween.OnComplete(() =>
      {
        _openTweens.Remove(panelId);
        Core.Event.EventManager.Instance.Emit(new PanelOpenedEvent(panelId));
      });
    }

    /// <summary>
    /// 带动画关闭面板
    /// </summary>
    public void ClosePanelWithAnimation(string panelId, Vector2 endPos, float duration = 0.3f, Ease ease = Ease.InBack, Action onComplete = null)
    {
      if (!_panels.TryGetValue(panelId, out var panel))
      {
        Debug.LogWarning($"[UIManager] Panel '{panelId}' not found!");
        return;
      }

      if (!panel.activeSelf) return;

      // 停止之前的动画
      if (_openTweens.TryGetValue(panelId, out var oldTween))
      {
        oldTween.Kill();
      }

      var rect = panel.GetComponent<RectTransform>();
      if (rect == null)
      {
        ClosePanel(panelId);
        return;
      }

      var tween = rect.DOAnchorPos(endPos, duration).SetEase(ease);
      _openTweens[panelId] = tween;

      tween.OnComplete(() =>
      {
        _openTweens.Remove(panelId);
        panel.SetActive(false);
        onComplete?.Invoke();
        Core.Event.EventManager.Instance.Emit(new PanelClosedEvent(panelId));
      });
    }

    /// <summary>
    /// 切换面板显示状态
    /// </summary>
    public void TogglePanel(string panelId)
    {
      if (!_panels.TryGetValue(panelId, out var panel))
      {
        Debug.LogWarning($"[UIManager] Panel '{panelId}' not found!");
        return;
      }

      if (panel.activeSelf)
      {
        ClosePanel(panelId);
      }
      else
      {
        OpenPanel(panelId);
      }
    }

    /// <summary>
    /// 关闭所有面板
    /// </summary>
    public void CloseAllPanels()
    {
      foreach (var kvp in _panels)
      {
        if (kvp.Value.activeSelf)
        {
          kvp.Value.SetActive(false);
          Core.Event.EventManager.Instance.Emit(new PanelClosedEvent(kvp.Key));
        }
      }
    }

    /// <summary>
    /// 设置选中对象
    /// </summary>
    public void SetSelectedGameObject(GameObject obj)
    {
      _eventSystem?.SetSelectedGameObject(obj);
    }

    /// <summary>
    /// 检查是否悬停在UI上
    /// </summary>
    public bool IsPointerOverUI()
    {
      return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
  }

  /// <summary>
  /// 面板打开事件
  /// </summary>
  public class PanelOpenedEvent : Core.Event.GameEventBase
  {
    public string PanelId { get; }

    public PanelOpenedEvent(string panelId)
    {
      PanelId = panelId;
    }
  }

  /// <summary>
  /// 面板关闭事件
  /// </summary>
  public class PanelClosedEvent : Core.Event.GameEventBase
  {
    public string PanelId { get; }

    public PanelClosedEvent(string panelId)
    {
      PanelId = panelId;
    }
  }

  /// <summary>
  /// UI面板基类 - 提供通用面板功能
  /// </summary>
  public abstract class UIPanelBase : Core.MonoBehaviourBase, IUIPanel
  {
    public string PanelId => _panelId;

    [SerializeField] protected string _panelId;

    public bool IsOpen => gameObject.activeSelf;

    protected virtual void Awake()
    {
      UIManager.Instance.RegisterPanel(_panelId, gameObject);
    }

    public virtual void Open()
    {
      UIManager.Instance.OpenPanel(_panelId);
    }

    public virtual void Close()
    {
      UIManager.Instance.ClosePanel(_panelId);
    }

    protected virtual void OnPanelOpened() { }
    protected virtual void OnPanelClosed() { }
  }
}
