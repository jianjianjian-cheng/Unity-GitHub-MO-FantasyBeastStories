using Core;
using UnityEngine;
using XLua;

public class LuaEnvManager
{
  private static LuaEnvManager _instance;
  public static LuaEnvManager Instance =>
      _instance ??= new LuaEnvManager();

  public LuaEnv LuaEnv { get; private set; }

  /// <summary>确保 LuaEnv 已初始化（懒加载）</summary>
  public void EnsureInitialized()
  {
    if (LuaEnv == null)
      Init();
  }

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

  /// <summary>
  /// 加载角色行为 Lua 表（Heroes/{characterName}）
  /// </summary>
  /// <summary>
  /// 规范化角色名：去掉 "Root" 后缀，使 "WizardBoyRoot" → "WizardBoy"
  /// </summary>
  private static string NormalizeCharacterName(string characterName)
  {
    if (string.IsNullOrEmpty(characterName))
      return characterName;
    if (characterName.EndsWith("Root"))
      return characterName.Substring(0, characterName.Length - 4);
    return characterName;
  }

  public LuaTable LoadHeroBehavior(string characterName)
  {
    EnsureInitialized();
    if (LuaEnv == null)
    {
      Debug.LogWarning($"[LuaEnvManager] LuaEnv 初始化失败，无法加载角色行为: {characterName}");
      return null;
    }

    string normalized = NormalizeCharacterName(characterName);
    try
    {
      var results = LuaEnv.DoString($"return require('Heroes.{normalized}')");
      if (results != null && results.Length > 0)
        return results[0] as LuaTable;
    }
    catch (System.Exception e)
    {
      Debug.LogWarning($"[LuaEnvManager] 加载角色行为 Lua 失败: Heroes.{normalized} | {e.Message}");
    }
    return null;
  }

  /// <summary>
  /// 加载攻击行为 Lua 表（Combat/{characterName}Attack）
  /// </summary>
  public LuaTable LoadAttackBehavior(string characterName)
  {
    EnsureInitialized();
    if (LuaEnv == null)
    {
      Debug.LogWarning($"[LuaEnvManager] LuaEnv 初始化失败，无法加载攻击行为: {characterName}");
      return null;
    }

    string normalized = NormalizeCharacterName(characterName);
    try
    {
      var results = LuaEnv.DoString($"return require('Combat.{normalized}Attack')");
      if (results != null && results.Length > 0)
        return results[0] as LuaTable;
    }
    catch (System.Exception e)
    {
      Debug.LogWarning($"[LuaEnvManager] 加载攻击行为 Lua 失败: Combat.{normalized}Attack | {e.Message}");
    }
    return null;
  }

  public void Dispose()
  {
    LuaEnv?.Dispose();
    LuaEnv = null;
    _instance = null;
  }
}
