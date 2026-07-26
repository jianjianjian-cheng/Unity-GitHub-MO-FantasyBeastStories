using System;
using Controllers.Combat;
using UnityEngine;
using XLua;

/// <summary>
/// 攻击行为 Lua 桥接器。
/// 调度 PerformAttack / UpdateEnemyTarget 到 Lua。
/// </summary>
public class AttackLuaBridge
{
    private LuaTable _behavior;
    private readonly string _characterName;
    private bool _luaEnabled = true;

    public AttackLuaBridge(string characterName)
    {
        _characterName = characterName;

        try
        {
            _behavior = LuaEnvManager.Instance.LoadAttackBehavior(characterName);
            if (_behavior != null)
                Debug.Log($"[AttackLuaBridge] {characterName} 攻击行为 Lua 加载成功");
            else
                Debug.Log($"[AttackLuaBridge] {characterName} 无攻击行为 Lua（使用 C# 默认）");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AttackLuaBridge] {characterName} Lua 加载失败: {e.Message}");
            _luaEnabled = false;
        }
    }

    public bool TryPerformAttack(AttackRangeBase range, GameObject target)
    {
        if (!_luaEnabled || _behavior == null)
            return false;

        LuaFunction fn = null;
        try
        {
            fn = _behavior.Get<LuaFunction>("PerformAttack");
            if (fn == null)
                return false;

            fn.Call(range, target);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[AttackLuaBridge] {_characterName}.PerformAttack 异常:\n" +
                $"  Error: {e.Message}\n"
            );
            _luaEnabled = false;
            return false;
        }
        finally
        {
            fn?.Dispose();
        }
    }

    /// <summary>尝试更新目标列表，返回 true 表示 Lua 处理了</summary>
    public bool TryUpdateEnemyTarget(AttackRangeBase range)
    {
        if (!_luaEnabled || _behavior == null)
            return false;

        LuaFunction fn = null;
        try
        {
            fn = _behavior.Get<LuaFunction>("UpdateEnemyTarget");
            if (fn == null)
                return false;

            fn.Call(range);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[AttackLuaBridge] {_characterName}.UpdateEnemyTarget 异常:\n" +
                $"  Error: {e.Message}\n"
            );
            _luaEnabled = false;
            return false;
        }
        finally
        {
            fn?.Dispose();
        }
    }

    /// <summary>热修复后重新加载</summary>
    public void Reload()
    {
        _luaEnabled = true;
        try
        {
            _behavior = LuaEnvManager.Instance.LoadAttackBehavior(_characterName);
            Debug.Log($"[AttackLuaBridge] {_characterName} 攻击行为重新加载");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AttackLuaBridge] 重新加载失败: {e.Message}");
            _luaEnabled = false;
        }
    }
}