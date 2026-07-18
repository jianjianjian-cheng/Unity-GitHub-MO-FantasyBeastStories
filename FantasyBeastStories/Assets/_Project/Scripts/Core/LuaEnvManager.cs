using System.IO;
using UnityEngine;
using XLua;

public class LuaEnvManager
{
  private static LuaEnvManager _instance;
  public static LuaEnvManager Instance =>
      _instance ??= new LuaEnvManager();

  public LuaEnv LuaEnv { get; private set; }

  // 热更新 AssetBundle 引用（由 HotfixManager 设置）
  private AssetBundle _hotfixBundle;

  // 自定义 Loader：优先级 AB → persistentDataPath → Resources
  public void Init()
  {
    LuaEnv = new LuaEnv();
    LuaEnv.AddLoader(CustomLoader);
    Debug.Log("[LuaEnvManager] Lua 环境初始化完成");
  }

  private byte[] CustomLoader(ref string filePath)
  {
    // Lua require 用 . 分隔模块路径，需转换为目录分隔符
    string relativePath = filePath.Replace('.', '/');

    // 0. 最高优先级：从热更新 AssetBundle 加载
    if (_hotfixBundle != null)
    {
      // AB 中资源名为 "XxxLua.lua.txt"（取模块最后一段 + .lua.txt）
      string fileName = relativePath.Contains("/")
          ? relativePath.Substring(relativePath.LastIndexOf('/') + 1)
          : relativePath;
      string assetName = fileName + ".lua.txt";

      var textAsset = _hotfixBundle.LoadAsset<TextAsset>(assetName);
      if (textAsset != null)
      {
        Debug.Log($"[LuaEnvManager] 从 AB 加载: {filePath} → {assetName}");
        return textAsset.bytes;
      }
    }

    // 1. 从热更新下载目录加载
    string hotfixPath = Path.Combine(Application.persistentDataPath, "Lua", relativePath + ".lua.txt");
    if (File.Exists(hotfixPath))
    {
      Debug.Log($"[LuaEnvManager] 从热更新目录加载: {hotfixPath}");
      return File.ReadAllBytes(hotfixPath);
    }

    // 2. 回退到 Resources（包体内保底）
    // .lua.txt 文件在 Unity 中 asset 名为 xxx.lua（.txt 被 Unity 截去）
    var resAsset = Resources.Load<TextAsset>("Lua/" + relativePath + ".lua");
    if (resAsset == null)
      resAsset = Resources.Load<TextAsset>("Lua/" + relativePath);
    if (resAsset != null)
    {
      Debug.Log($"[LuaEnvManager] 从 Resources 加载: {filePath}");
      return resAsset.bytes;
    }

    Debug.LogWarning($"[LuaEnvManager] 无法找到 Lua 文件: {filePath}");
    return null;
  }

  // 设置热更新 AssetBundle（由 HotfixManager 调用）
  public void SetAssetBundleLoader(AssetBundle hotfixBundle)
  {
    if (hotfixBundle == null) return;
    _hotfixBundle = hotfixBundle;
    Debug.Log("[LuaEnvManager] 热更新 AssetBundle 已设置");
  }

  public void Dispose()
  {
    LuaEnv?.Dispose();
    LuaEnv = null;
    _hotfixBundle = null;
    _instance = null;
  }
}