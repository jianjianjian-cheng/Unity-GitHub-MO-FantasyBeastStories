using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Core.Channels;
using UI.Framework.Animation;
using UI.Framework.Manager;
using UnityEngine;

namespace UI.Framework.Base
{
  public abstract class UIScreen : MonoBehaviour
  {
    [Header("UIScreen 基础设置")]
    [SerializeField] protected string screenId;
    [SerializeField] protected UILayer defaultLayer = UILayer.Normal;
    [SerializeField] protected bool useMask = true;
    [SerializeField] protected bool closeOnEsc = true;
    [SerializeField] protected bool destroyOnClose = false;
    [SerializeField] protected UIAnimationBase openAnimation;
    [SerializeField] protected UIAnimationBase closeAnimation;

    public bool IsOpen { get; private set; }
    public bool IsAnimating { get; private set; }
    public UILayer CurrentLayer { get; private set; }
    public string ScreenId => screenId;
    public UILayer DefaultLayer => defaultLayer;
    public bool UseMask => useMask;

    protected Canvas _canvas;
    protected CanvasGroup _canvasGroup;
    protected List<MonoBehaviour> _childWidgets = new();

    protected virtual void Awake()
    {
      _canvas = GetComponent<Canvas>();
      _canvasGroup = GetComponent<CanvasGroup>();

      if (_canvasGroup == null)
        _canvasGroup = gameObject.AddComponent<CanvasGroup>();

      gameObject.SetActive(false);
      CollectChildWidgets();
    }

    protected virtual void CollectChildWidgets()
    {
      var widgets = GetComponentsInChildren<UIWidget>(true);
      foreach (var widget in widgets)
      {
        _childWidgets.Add(widget);
      }
    }

    protected virtual void Start() { }

    protected virtual void OnEnable()
    {
      SubscribeEvents();
    }

    protected virtual void OnDisable()
    {
      UnsubscribeEvents();
    }

    protected virtual void Update()
    {
      if (closeOnEsc && IsOpen && UnityEngine.Input.GetKeyDown(KeyCode.Escape))
      {
        CloseSelf();
      }
    }

    public async void Open()
    {
      if (IsOpen) return;
      IsAnimating = true;

      gameObject.SetActive(true);

      foreach (var widget in _childWidgets)
      {
        if (widget is UIWidget w)
          w.OnScreenOpened();
      }

      OnBeforeOpen();

      if (openAnimation != null)
        await openAnimation.PlayAsync(gameObject);

      IsOpen = true;
      IsAnimating = false;
      OnAfterOpen();
    }

    public async void Close()
    {
      if (!IsOpen) return;
      IsAnimating = true;

      OnBeforeClose();

      if (closeAnimation != null)
        await closeAnimation.PlayAsync(gameObject);

      IsOpen = false;
      IsAnimating = false;
      OnAfterClose();

      foreach (var widget in _childWidgets)
      {
        if (widget is UIWidget w)
          w.OnScreenClosed();
      }

      gameObject.SetActive(false);

      if (destroyOnClose)
        Destroy(gameObject);
    }

    protected virtual void OnBeforeOpen() { }
    protected virtual void OnAfterOpen() { }
    protected virtual void OnBeforeClose() { }
    protected virtual void OnAfterClose() { }

    protected virtual void SubscribeEvents() { }
    protected virtual void UnsubscribeEvents() { }

    protected T GetChannel<T>() where T : ScriptableObject
    {
      var container = EventChannelLocator.MainContainer;
      var fields = typeof(EventChannelContainerSO).GetFields();
      foreach (var field in fields)
      {
        if (field.FieldType == typeof(T))
        {
          return field.GetValue(container) as T;
        }
      }
      return null;
    }

    protected void CloseSelf() => UIManager.Instance.Close(screenId);

    public void SetLayer(UILayer layer)
    {
      CurrentLayer = layer;
      // +50 让画面排在层级 Canvas（遮罩所在）之上，同时仍然在其他层级 Canvas 之下
      if (_canvas != null)
        _canvas.sortingOrder = (int)layer * 100 + 50;
    }

    public CanvasGroup GetCanvasGroup() => _canvasGroup;
  }
}