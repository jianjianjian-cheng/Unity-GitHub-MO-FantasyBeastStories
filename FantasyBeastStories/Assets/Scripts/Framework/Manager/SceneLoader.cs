using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace Framework
{
  /// <summary>
  /// 场景加载器 - 异步场景加载管理
  /// </summary>
  public class SceneLoader : Core.MonoSingleton<SceneLoader>
  {
    private readonly Dictionary<int, AsyncOperation> _loadingOperations = new Dictionary<int, AsyncOperation>();
    private float _loadingProgress = 0f;
    private bool _isLoading = false;
    private Action _onLoadComplete;

    public float LoadingProgress => _loadingProgress;
    public bool IsLoading => _isLoading;

    /// <summary>
    /// 异步加载场景
    /// </summary>
    public void LoadSceneAsync(int sceneIndex, Action onComplete = null, bool showLoadingUI = true)
    {
      if (_isLoading)
      {
        Debug.LogWarning("[SceneLoader] Already loading a scene!");
        return;
      }

      _isLoading = true;
      _loadingProgress = 0f;
      _onLoadComplete = onComplete;

      StartCoroutine(LoadSceneCoroutine(sceneIndex, showLoadingUI));
    }

    private IEnumerator LoadSceneCoroutine(int sceneIndex, bool showLoadingUI)
    {
      string sceneName = SceneManager.GetSceneByBuildIndex(sceneIndex).name;

      // 通知加载开始
      Core.Event.EventManager.Instance.Emit(new SceneLoadStartedEvent(sceneIndex, sceneName));

      if (showLoadingUI)
      {
        ShowLoadingScreen();
      }

      // 激活额外场景（如果需要）
      AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Additive);

      if (loadOp == null)
      {
        Debug.LogError($"[SceneLoader] Failed to load scene {sceneIndex}!");
        _isLoading = false;
        yield break;
      }

      _loadingOperations[sceneIndex] = loadOp;
      loadOp.allowSceneActivation = false;

      // 模拟加载进度（因为Unity的loadOp.progress在allowSceneActivation=false时卡在0.9）
      float fakeProgress = 0f;
      while (!loadOp.isDone)
      {
        // 合并真实进度和模拟进度
        _loadingProgress = Mathf.Lerp(fakeProgress, loadOp.progress, 0.8f);

        if (_loadingProgress >= 0.9f && fakeProgress < 0.9f)
        {
          fakeProgress += Time.deltaTime * 0.5f; // 模拟加载
          _loadingProgress = fakeProgress;
        }

        yield return null;
      }

      _loadingProgress = 1f;
      loadOp.allowSceneActivation = true;
      _isLoading = false;

      if (showLoadingUI)
      {
        HideLoadingScreen(() =>
        {
          FinalizeLoad(sceneIndex, sceneName);
        });
      }
      else
      {
        FinalizeLoad(sceneIndex, sceneName);
      }
    }

    private void FinalizeLoad(int sceneIndex, string sceneName)
    {
      _loadingOperations.Remove(sceneIndex);

      // 通知加载完成
      Core.Event.EventManager.Instance.Emit(new SceneLoadCompletedEvent(sceneIndex, sceneName));

      _onLoadComplete?.Invoke();
      _onLoadComplete = null;
    }

    /// <summary>
    /// 卸载指定场景
    /// </summary>
    public void UnloadSceneAsync(int sceneIndex, Action onComplete = null)
    {
      StartCoroutine(UnloadSceneCoroutine(sceneIndex, onComplete));
    }

    private IEnumerator UnloadSceneCoroutine(int sceneIndex, Action onComplete)
    {
      AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(sceneIndex);

      if (unloadOp != null)
      {
        while (!unloadOp.isDone)
        {
          yield return null;
        }
      }

      onComplete?.Invoke();
    }

    /// <summary>
    /// 获取当前活跃场景索引
    /// </summary>
    public int GetCurrentSceneIndex()
    {
      return SceneManager.GetActiveScene().buildIndex;
    }

    /// <summary>
    /// 获取当前活跃场景名称
    /// </summary>
    public string GetCurrentSceneName()
    {
      return SceneManager.GetActiveScene().name;
    }

    private void ShowLoadingScreen()
    {
      // 可以在这里显示Loading UI
      // 例如：UIManager.Instance.ShowPanel<LoadingPanel>();
    }

    private void HideLoadingScreen(Action onComplete)
    {
      // 延迟一帧确保场景已激活
      DOVirtual.DelayedCall(0.1f, () =>
      {
        onComplete?.Invoke();
      });
    }

    /// <summary>
    /// 重新加载当前场景
    /// </summary>
    public void ReloadCurrentScene(Action onComplete = null)
    {
      int currentIndex = GetCurrentSceneIndex();
      LoadSceneAsync(currentIndex, onComplete);
    }
  }
}
