using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 热更新管理器。负责下载 AssetBundle 并加载 Lua + 图片资源。
/// 挂载在 GameManager 或启动场景的 GameObject 上。
/// 开发阶段：未配置 CDN 时自动跳过，使用 Resources 保底。
/// </summary>
public class HotfixManager : MonoBehaviour
{
  [Header("热更新配置")]
  [SerializeField] private string hotfixUrl = "https://a.unity.cn/client_api/v1/buckets/9a90add4-6c36-4796-a3ea-9114ea7561dc/release_by_badge/latest/content/rune_hotfix.ab";
  [SerializeField] private string bundleName = "rune_hotfix";
  [Tooltip("开发模式：直接从 Build/Hotfix/ 目录加载 AB，无需 CDN 下载")]
  [SerializeField] private bool devMode = false;

  public AssetBundle HotfixBundle { get; private set; }

  /// <summary>从 CDN 下载并加载 AssetBundle（开发模式直接从本地构建目录加载）</summary>
  public IEnumerator DownloadAndLoad()
  {
    string localPath = Path.Combine(Application.persistentDataPath, bundleName + ".ab");

    // 开发模式：直接从 Build/Hotfix/ 加载
    if (devMode)
    {
      string devPath = Path.Combine(Application.dataPath, "..", "Build", "Hotfix", bundleName);
      if (File.Exists(devPath))
      {
        Debug.Log($"[HotfixManager] 开发模式：从本地构建目录加载 {devPath}");
        LoadBundle(devPath);
        yield break;
      }
      Debug.LogWarning($"[HotfixManager] 开发模式：未找到本地 AB (路径: {devPath})，请先执行 Tools→Build Hotfix AssetBundle");
    }

    // 检查是否配置了有效的 CDN 地址
    bool isCDNConfigured = !string.IsNullOrEmpty(hotfixUrl)
                        && !hotfixUrl.Contains("your-cdn.com");

    if (isCDNConfigured)
    {
      bool needUpdate = !File.Exists(localPath);

      if (needUpdate)
      {
        Debug.Log($"[HotfixManager] 开始下载热更新包: {hotfixUrl}");

        using (var request = UnityWebRequest.Get(hotfixUrl))
        {
          yield return request.SendWebRequest();

          if (request.result == UnityWebRequest.Result.Success)
          {
            File.WriteAllBytes(localPath, request.downloadHandler.data);
            Debug.Log($"[HotfixManager] 热更新包下载完成: {localPath}");
          }
          else
          {
            Debug.LogWarning($"[HotfixManager] 下载失败: {request.error}");
          }
        }
      }
      else
      {
        Debug.Log("[HotfixManager] 无需更新，使用本地缓存");
      }
    }

    // 加载 AssetBundle
    if (File.Exists(localPath))
    {
      LoadBundle(localPath);
    }
    else
    {
      Debug.Log("[HotfixManager] 本地无热更新包，Lua 从 Resources 加载");
    }
  }

  private void LoadBundle(string path)
  {
    try
    {
      HotfixBundle = AssetBundle.LoadFromFile(path);
      if (HotfixBundle != null)
      {
        LuaEnvManager.Instance.SetAssetBundleLoader(HotfixBundle);
        Debug.Log("[HotfixManager] AssetBundle 加载成功");
      }
    }
    catch (System.Exception e)
    {
      Debug.LogWarning($"[HotfixManager] AssetBundle 加载失败: {e.Message}");
    }
  }

  /// <summary>从 AssetBundle 加载符文图标，失败时回退到 icon 字段</summary>
  public Sprite LoadRuneIcon(string iconName, Sprite fallbackIcon)
  {
    if (string.IsNullOrEmpty(iconName))
      return fallbackIcon;

    if (HotfixBundle != null)
    {
      var sprite = HotfixBundle.LoadAsset<Sprite>(iconName);
      if (sprite != null)
      {
        Debug.Log($"[HotfixManager] 从 AB 加载图标: {iconName}");
        return sprite;
      }
    }

    return fallbackIcon;
  }

  private void OnDestroy()
  {
    HotfixBundle?.Unload(false);
  }
}