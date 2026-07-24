using System.Collections.Generic;
using UI.Framework.Base;
using UI.Framework.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Framework.Manager
{
  public class UIManager : MonoBehaviour
  {
    private static UIManager _instance;

    public static UIManager Instance
    {
      get
      {
        if (_instance == null)
        {
          GameObject go = new GameObject("UIManager");
          _instance = go.AddComponent<UIManager>();
          _instance.Initialize();
        }
        return _instance;
      }
    }

    public static bool HasInstance => _instance != null;

    private UINavigationStack _navigationStack = new();
    private Dictionary<string, UIScreen> _registeredScreens = new();
    private Dictionary<UILayer, Canvas> _layerCanvases = new();
    private Dictionary<UILayer, GameObject> _layerMasks = new();

    private void Awake()
    {
      if (_instance == null)
      {
        _instance = this;
        Initialize();
      }
      else if (_instance != this)
      {
        Destroy(gameObject);
      }
    }

    private void Initialize()
    {
      CreateEventSystem();
    }

    private void EnsureLayerCanvas(UILayer layer)
    {
      if (_layerCanvases.ContainsKey(layer))
        return;

      GameObject canvasObj = new GameObject(layer.ToLayerName() + "Canvas");
      canvasObj.transform.SetParent(transform);

      Canvas canvas = canvasObj.AddComponent<Canvas>();
      canvas.renderMode = RenderMode.ScreenSpaceOverlay;
      canvas.sortingOrder = layer.ToSortingOrder();

      CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
      scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
      scaler.referenceResolution = new Vector2(2560, 1440);
      scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
      scaler.matchWidthOrHeight = 0.5f;

      canvasObj.AddComponent<GraphicRaycaster>();

      _layerCanvases[layer] = canvas;
    }

    private void CreateEventSystem()
    {
      if (FindObjectOfType<EventSystem>() != null)
        return;

      GameObject esObj = new GameObject(UIConstants.EventSystemName);
      esObj.AddComponent<EventSystem>();
      esObj.AddComponent<StandaloneInputModule>();
      esObj.transform.SetParent(transform);
      Debug.Log("[UIManager] 自动创建 EventSystem");
    }

    private GameObject CreateMask(UILayer layer)
    {
      if (_layerMasks.TryGetValue(layer, out var existing))
        return existing;

      EnsureLayerCanvas(layer);

      GameObject maskObj = new GameObject("Mask_" + layer.ToLayerName());

      Image maskImage = maskObj.AddComponent<Image>();
      maskImage.color = new Color(0, 0, 0, 0.5f);

      RectTransform rect = maskObj.GetComponent<RectTransform>();
      rect.anchorMin = Vector2.zero;
      rect.anchorMax = Vector2.one;
      rect.offsetMin = Vector2.zero;
      rect.offsetMax = Vector2.zero;

      maskObj.transform.SetParent(_layerCanvases[layer].transform);
      maskObj.transform.SetAsFirstSibling();
      maskObj.SetActive(false);

      _layerMasks[layer] = maskObj;

      return maskObj;
    }

    public void RegisterScreen(UIScreen screen)
    {
      if (string.IsNullOrEmpty(screen.ScreenId))
      {
        Debug.LogError("UIManager: 注册屏幕失败，screenId为空");
        return;
      }

      if (_registeredScreens.ContainsKey(screen.ScreenId))
      {
        Debug.LogWarning($"UIManager: 屏幕 {screen.ScreenId} 已存在，将被替换");
      }

      _registeredScreens[screen.ScreenId] = screen;
      EnsureLayerCanvas(screen.DefaultLayer);
      screen.SetLayer(screen.DefaultLayer);
      screen.transform.SetParent(_layerCanvases[screen.DefaultLayer].transform, false);
      screen.gameObject.SetActive(false);
    }

    public void Open(string screenId)
    {
      if (!_registeredScreens.TryGetValue(screenId, out var screen))
      {
        Debug.LogError($"UIManager: 未找到屏幕 {screenId}");
        return;
      }

      Open(screen);
    }

    public void Open(UIScreen screen)
    {
      if (screen == null)
        return;

      if (_navigationStack.Contains(screen))
      {
        Debug.LogWarning($"UIManager: 屏幕 {screen.ScreenId} 已在导航栈中");
        return;
      }

      UIScreen current = _navigationStack.CurrentScreen;
      if (current != null)
      {
        current.gameObject.SetActive(false);
      }

      _navigationStack.Push(screen);

      if (screen.UseMask)
      {
        ShowMask(screen.CurrentLayer);
      }

      screen.Open();
    }

    public void Close(string screenId)
    {
      if (!_registeredScreens.TryGetValue(screenId, out var screen))
      {
        Debug.LogError($"UIManager: 未找到屏幕 {screenId}");
        return;
      }

      Close(screen);
    }

    public void Close(UIScreen screen)
    {
      if (screen == null)
        return;

      if (!_navigationStack.Contains(screen))
      {
        Debug.LogWarning($"UIManager: 屏幕 {screen.ScreenId} 不在导航栈中");
        return;
      }

      screen.Close();
      _navigationStack.Remove(screen);

      if (screen.UseMask)
      {
        HideMask(screen.CurrentLayer);
      }

      UIScreen current = _navigationStack.CurrentScreen;
      if (current != null)
      {
        current.gameObject.SetActive(true);
      }
    }

    public void CloseCurrent()
    {
      UIScreen current = _navigationStack.CurrentScreen;
      if (current != null)
      {
        Close(current);
      }
    }

    public void GoBack()
    {
      UIScreen current = _navigationStack.CurrentScreen;
      if (current != null)
      {
        Close(current);
      }
    }

    public void ClearAll()
    {
      foreach (var screen in _registeredScreens.Values)
      {
        if (screen != null)
        {
          screen.Close();
        }
      }

      _navigationStack.Clear();

      foreach (var mask in _layerMasks.Values)
      {
        if (mask != null)
          mask.SetActive(false);
      }
    }

    private void ShowMask(UILayer layer)
    {
      GameObject mask = CreateMask(layer);
      mask.SetActive(true);
    }

    private void HideMask(UILayer layer)
    {
      if (_layerMasks.TryGetValue(layer, out var mask))
      {
        mask.SetActive(false);
      }
    }

    public bool IsScreenOpen(string screenId)
    {
      if (_registeredScreens.TryGetValue(screenId, out var screen))
      {
        return screen.IsOpen;
      }
      return false;
    }

    public UIScreen GetScreen(string screenId)
    {
      _registeredScreens.TryGetValue(screenId, out var screen);
      return screen;
    }

    public UIScreen GetCurrentScreen()
    {
      return _navigationStack.CurrentScreen;
    }

    public T GetScreen<T>() where T : UIScreen
    {
      foreach (var screen in _registeredScreens.Values)
      {
        if (screen is T typedScreen)
          return typedScreen;
      }
      return null;
    }

    public void SetLayer(UIScreen screen, UILayer layer)
    {
      if (screen == null)
        return;

      screen.SetLayer(layer);
    }

    public void UnregisterScreen(UIScreen screen)
    {
      if (screen == null || string.IsNullOrEmpty(screen.ScreenId))
        return;

      if (_registeredScreens.TryGetValue(screen.ScreenId, out var registered) && registered == screen)
      {
        _registeredScreens.Remove(screen.ScreenId);
        _navigationStack.Remove(screen);
      }
    }

    private void OnDestroy()
    {
      if (_instance == this)
        _instance = null;
    }
  }
}