using System.Collections.Generic;
using System.Linq;
using UI.Framework.Base;

namespace UI.Framework.Manager
{
  public class UINavigationStack
  {
    private Stack<UIScreen> _screenStack = new();
    private Dictionary<UILayer, Stack<UIScreen>> _layerStacks = new();

    public UIScreen CurrentScreen => _screenStack.Count > 0 ? _screenStack.Peek() : null;
    public int Count => _screenStack.Count;

    public void Push(UIScreen screen)
    {
      _screenStack.Push(screen);

      if (!_layerStacks.ContainsKey(screen.CurrentLayer))
        _layerStacks[screen.CurrentLayer] = new Stack<UIScreen>();

      _layerStacks[screen.CurrentLayer].Push(screen);
    }

    public UIScreen Pop()
    {
      if (_screenStack.Count == 0)
        return null;

      UIScreen screen = _screenStack.Pop();

      if (_layerStacks.TryGetValue(screen.CurrentLayer, out var stack))
      {
        if (stack.Count > 0)
          stack.Pop();
      }

      return screen;
    }

    public bool Contains(string screenId)
    {
      foreach (var screen in _screenStack)
      {
        if (screen.ScreenId == screenId)
          return true;
      }
      return false;
    }

    public bool Contains(UIScreen screen)
    {
      return _screenStack.Contains(screen);
    }

    public void Remove(UIScreen screen)
    {
      _screenStack = new Stack<UIScreen>(
          new Stack<UIScreen>(_screenStack).Where(s => s != screen)
      );

      if (_layerStacks.TryGetValue(screen.CurrentLayer, out var stack))
      {
        stack = new Stack<UIScreen>(
            new Stack<UIScreen>(stack).Where(s => s != screen)
        );
        _layerStacks[screen.CurrentLayer] = stack;
      }
    }

    public void Clear()
    {
      _screenStack.Clear();

      foreach (var pair in _layerStacks)
      {
        pair.Value.Clear();
      }

      _layerStacks.Clear();
    }

    public UIScreen GetScreen(string screenId)
    {
      foreach (var screen in _screenStack)
      {
        if (screen.ScreenId == screenId)
          return screen;
      }
      return null;
    }

    public Stack<UIScreen> GetLayerStack(UILayer layer)
    {
      if (_layerStacks.TryGetValue(layer, out var stack))
        return stack;
      return new Stack<UIScreen>();
    }
  }
}