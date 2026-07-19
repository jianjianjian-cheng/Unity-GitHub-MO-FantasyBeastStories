using System.Collections.Generic;
using System.Linq;
using UI.Framework.Base;

namespace UI.Framework.Manager
{
  public class UINavigationStack
  {
    private Stack<UIScreen> _screenStack = new();

    public UIScreen CurrentScreen => _screenStack.Count > 0 ? _screenStack.Peek() : null;
    public int Count => _screenStack.Count;

    public void Push(UIScreen screen)
    {
      _screenStack.Push(screen);
    }

    public UIScreen Pop()
    {
      return _screenStack.Count == 0 ? null : _screenStack.Pop();
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
    }

    public void Clear()
    {
      _screenStack.Clear();
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
  }
}
