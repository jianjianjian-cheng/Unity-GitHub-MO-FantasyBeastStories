using System;
using Controllers.Battle;
using UnityEngine;
using XLua;
using Controllers.Game;
using Core.SharedModel;

namespace Controllers.Character
{
  /// <summary>
  /// 攻击行为 Lua 桥接器。
  /// 使用强类型委托替代 LuaFunction.Call()，零 GC、类型安全。
  /// </summary>
  public class AttackLuaBridge
  {
      private LuaTable _behavior;
      private readonly string _characterName;
      private bool _luaEnabled = true;

      // 缓存委托
      private LuaAttackAction _performAttack;
      private LuaBoolAction _updateEnemyTarget;
      private bool _delegatesCached;

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

      private void EnsureDelegatesCached()
      {
          if (_delegatesCached || _behavior == null) return;
          _delegatesCached = true;

          try
          {
              _performAttack = _behavior.Get<LuaAttackAction>("PerformAttack");
              _updateEnemyTarget = _behavior.Get<LuaBoolAction>("UpdateEnemyTarget");
          }
          catch (Exception e)
          {
              Debug.LogWarning($"[AttackLuaBridge] 委托缓存失败: {e.Message}");
          }
      }

      public bool TryPerformAttack(AttackRangeBase range, GameObject target)
      {
          if (!_luaEnabled || _behavior == null) return false;
          EnsureDelegatesCached();
          if (_performAttack == null) return false;

          try
          {
              _performAttack(range, target);
              return true;
          }
          catch (Exception e)
          {
              Debug.LogError($"[AttackLuaBridge] {_characterName}.PerformAttack 异常: {e.Message}");
              _luaEnabled = false;
              return false;
          }
      }

      public bool TryUpdateEnemyTarget(AttackRangeBase range)
      {
          if (!_luaEnabled || _behavior == null) return false;
          EnsureDelegatesCached();
          if (_updateEnemyTarget == null) return false;

          try
          {
              return _updateEnemyTarget(range);
          }
          catch (Exception e)
          {
              Debug.LogError($"[AttackLuaBridge] {_characterName}.UpdateEnemyTarget 异常: {e.Message}");
              _luaEnabled = false;
              return false;
          }
      }

      public void Reload()
      {
          _luaEnabled = true;
          _delegatesCached = false;
          _performAttack = null;
          _updateEnemyTarget = null;

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
}
