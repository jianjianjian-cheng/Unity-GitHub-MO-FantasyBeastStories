using Core;
using UnityEngine;
using XLua;

public class LuaEnvManager
{
  private static LuaEnvManager _instance;
  public static LuaEnvManager Instance =>
      _instance ??= new LuaEnvManager();

  public LuaEnv LuaEnv { get; private set; }

  // 自定义 Loader：通过 Addressables 加载 Lua 脚本（支持远程热更）
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

    // 通过 Addressables 加载（远程热更 bundle 优先于本地包体）
    var resAsset = AssetLoader.TryLoadAsset<TextAsset>("Lua/" + relativePath);
    if (resAsset == null)
      resAsset = AssetLoader.TryLoadAsset<TextAsset>("Lua/" + relativePath + ".lua");
    if (resAsset != null)
    {
      Debug.Log($"[LuaEnvManager] 从 Addressables 加载: {filePath}");
      return resAsset.bytes;
    }

    Debug.LogWarning($"[LuaEnvManager] 无法找到 Lua 文件: {filePath}");
    return null;
  }

  public void Dispose()
  {
    LuaEnv?.Dispose();
    LuaEnv = null;
    _instance = null;
  }
}
