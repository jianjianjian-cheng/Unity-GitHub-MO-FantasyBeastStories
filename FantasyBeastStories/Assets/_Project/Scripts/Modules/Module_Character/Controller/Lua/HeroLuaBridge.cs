using System;
using Controllers.Battle;
using Core.Channels.Player;
using UnityEngine;
using XLua;
using Core.SharedModel;
using Controllers.Game;

namespace Controllers.Character
{
  /// <summary>
  /// 角色行为 Lua 桥接器。
  /// 使用强类型委托替代 LuaFunction.Call()，零 GC、类型安全。
  /// 报错后自动禁用 Lua 调用，降级为 C# 默认行为。
  /// </summary>
  public class HeroLuaBridge
  {
      private LuaTable _behavior;
      private readonly string _characterName;
      private bool _luaEnabled = true;

      // 缓存委托，避免每次调用都 Get<>()
      private LuaVoidAction _onStart;
      private LuaSkillQueryAction _onSkillQuery;
      private LuaElementAction _onSwitchElement;
      private LuaBoolElementAction _onUnlockElement;
      private LuaSceneAction _onSceneLoaded;
      private LuaVoidAction _onDeath;
      private LuaElementAction _onInitElementPool;
      private bool _delegatesCached;

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

      /// <summary>缓存所有委托（只执行一次）</summary>
      private void EnsureDelegatesCached()
      {
          if (_delegatesCached || _behavior == null) return;
          _delegatesCached = true;

          try
          {
              _onStart = _behavior.Get<LuaVoidAction>("OnStart");
              _onSkillQuery = _behavior.Get<LuaSkillQueryAction>("OnSkillQuery");
              _onSwitchElement = _behavior.Get<LuaElementAction>("OnSwitchElement");
              _onUnlockElement = _behavior.Get<LuaBoolElementAction>("OnUnlockElement");
              _onSceneLoaded = _behavior.Get<LuaSceneAction>("OnSceneLoaded");
              _onDeath = _behavior.Get<LuaVoidAction>("OnDeath");
              _onInitElementPool = _behavior.Get<LuaElementAction>("OnInitElementPool");
          }
          catch (Exception e)
          {
              Debug.LogWarning($"[HeroLuaBridge] 委托缓存失败: {e.Message}");
          }
      }

      // ===== 角色生命周期回调 =====

      public void OnStart(PlayerController player)
      {
          if (!_luaEnabled || _behavior == null) return;
          EnsureDelegatesCached();
          if (_onStart != null)
          {
              try { _onStart(player); }
              catch (Exception e) { Debug.LogError($"[HeroLuaBridge] OnStart 异常: {e.Message}"); }
          }
      }

      public bool OnSkillQuery(PlayerController player, SkillQueryData data)
      {
          if (!_luaEnabled || _behavior == null) return false;
          EnsureDelegatesCached();
          if (_onSkillQuery != null)
          {
              try { _onSkillQuery(player, data); return true; }
              catch (Exception e) { Debug.LogError($"[HeroLuaBridge] OnSkillQuery 异常: {e.Message}"); }
          }
          return false;
      }

      public void OnSwitchElement(PlayerController player, Element element)
      {
          if (!_luaEnabled || _behavior == null) return;
          EnsureDelegatesCached();
          if (_onSwitchElement != null)
          {
              try { _onSwitchElement(player, (int)element); }
              catch (Exception e) { Debug.LogError($"[HeroLuaBridge] OnSwitchElement 异常: {e.Message}"); }
          }
      }

      public bool OnUnlockElement(PlayerController player, Element element)
      {
          if (!_luaEnabled || _behavior == null) return false;
          EnsureDelegatesCached();
          if (_onUnlockElement != null)
          {
              try { return _onUnlockElement(player, (int)element); }
              catch (Exception e) { Debug.LogError($"[HeroLuaBridge] OnUnlockElement 异常: {e.Message}"); }
          }
          return false;
      }

      public void OnSceneLoaded(PlayerController player, int sceneIndex)
      {
          if (!_luaEnabled || _behavior == null) return;
          EnsureDelegatesCached();
          if (_onSceneLoaded != null)
          {
              try { _onSceneLoaded(player, sceneIndex); }
              catch (Exception e) { Debug.LogError($"[HeroLuaBridge] OnSceneLoaded 异常: {e.Message}"); }
          }
      }

      public void OnDeath(PlayerController player)
      {
          if (!_luaEnabled || _behavior == null) return;
          EnsureDelegatesCached();
          if (_onDeath != null)
          {
              try { _onDeath(player); }
              catch (Exception e) { Debug.LogError($"[HeroLuaBridge] OnDeath 异常: {e.Message}"); }
          }
      }

      public void OnInitElementPool(PlayerController player, int elementInt)
      {
          if (!_luaEnabled || _behavior == null) return;
          EnsureDelegatesCached();
          if (_onInitElementPool != null)
          {
              try { _onInitElementPool(player, elementInt); }
              catch (Exception e) { Debug.LogError($"[HeroLuaBridge] OnInitElementPool 异常: {e.Message}"); }
          }
      }

      /// <summary>热修复后重新加载</summary>
      public void Reload()
      {
          _luaEnabled = true;
          _delegatesCached = false;
          _onStart = null;
          _onSkillQuery = null;
          _onSwitchElement = null;
          _onUnlockElement = null;
          _onSceneLoaded = null;
          _onDeath = null;
          _onInitElementPool = null;

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
}
