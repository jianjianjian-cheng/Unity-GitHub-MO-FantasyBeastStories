using System;
using Controllers.Character;
using Controllers.Combat;
using Core.Channels.Player;
using UnityEngine;
using XLua;

/// <summary>
/// 角色行为 Lua 桥接器。
/// 统一调度所有角色虚方法到 Lua。
/// 让AI为我编写了预防机制：报错后自动禁用 Lua 调用，降级为 C# 默认行为。
/// </summary>
public class HeroLuaBridge
{
    private LuaTable _behavior;
    private readonly string _characterName;
    private bool _luaEnabled = true;

    public HeroLuaBridge(string characterName)
    {
        _characterName = characterName;

        try
        {
            _behavior = LuaEnvManager.Instance.LoadHeroBehavior(characterName);
            if (_behavior != null)
                Debug.Log($"[HeroLuaBridge] 角色 {characterName} Lua 行为加载成功");
            else
                Debug.Log($"[HeroLuaBridge] 角色 {characterName} 无 Lua 行为（使用 C# 默认）");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[HeroLuaBridge] 角色 {characterName} Lua 加载失败: {e.Message}");
            _luaEnabled = false;
        }
    }

    /// <summary>统一调用入口，返回 true 表示 Lua 处理了该回调</summary>
    private bool SafeCall(string functionName, params object[] args)
    {
        if (!_luaEnabled || _behavior == null)
            return false;

        LuaFunction fn = null;
        try
        {
            fn = _behavior.Get<LuaFunction>(functionName);
            if (fn == null)
                return false; // 该角色未实现此回调，静默跳过

            fn.Call(args);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[HeroLuaBridge] 角色({_characterName}).{functionName} 异常:\n" +
                $"  Error: {e.Message}\n" +
                $"  StackTrace: {e.StackTrace}"
            );
            _luaEnabled = false; // 防止反复报错刷屏
            return false;
        }
        finally
        {
            fn?.Dispose();
        }
    }

    // ===== 角色生命周期回调 =====

    public void OnStart(PlayerController player)
        => SafeCall("OnStart", player);

    public bool OnSkillQuery(PlayerController player, SkillQueryData data)
        => SafeCall("OnSkillQuery", player, data);

    public void OnSwitchElement(PlayerController player, Element element)
        => SafeCall("OnSwitchElement", player, (int)element);

    public bool OnUnlockElement(PlayerController player, Element element)
        => SafeCall("OnUnlockElement", player, (int)element);

    public void OnSceneLoaded(PlayerController player, int sceneIndex)
        => SafeCall("OnSceneLoaded", player, sceneIndex);

    public void OnDeath(PlayerController player)
        => SafeCall("OnDeath", player);

    public void OnInitElementPool(PlayerController player, int elementInt)
        => SafeCall("OnInitElementPool", player, elementInt);

    /// <summary>热修复后重新加载</summary>
    public void Reload()
    {
        _luaEnabled = true;
        try
        {
            _behavior = LuaEnvManager.Instance.LoadHeroBehavior(_characterName);
            Debug.Log($"[HeroLuaBridge] 角色 {_characterName} Lua 行为重新加载");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[HeroLuaBridge] 重新加载失败: {e.Message}");
            _luaEnabled = false;
        }
    }
}