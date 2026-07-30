using System;
using System.Collections.Generic;
using UnityEngine;
using XLua;

/// <summary>
/// XLua 代码生成配置
/// 
/// XLua asmdef 只引用 Core 和 Framework（不能引用 Modules，会循环依赖）。
/// Modules 中的类型（GamePauseManager, GameManager 等）在运行时通过反射访问。
/// 
/// 生成代码命令：XLua > Generate Code
/// </summary>
static class FantasyBeastLuaConfig
{
    [LuaCallCSharp]
    public static List<Type> LuaCallCSharp = new List<Type>()
    {
        // ── Core.SharedModel ──
        typeof(Core.SharedModel.Element),
        typeof(Core.SharedModel.EnemyState),
        typeof(Core.SharedModel.NetworkTarget),
        typeof(Core.SharedModel.SkillQueryType),
        typeof(Core.SharedModel.SkillQueryData),
        typeof(Core.SharedModel.AttributePlayerBase),
        typeof(Core.SharedModel.PlayerMovementData),
        typeof(Core.SharedModel.PlayerAttributeConfigSO),
        typeof(Core.SharedModel.CardConfigSO),

        // ── Core.Network ──
        typeof(Core.Network.NetworkServiceLocator),

        // ── Core 基础设施 ──
        typeof(Core.EventChannelLocator),
        typeof(Core.ServiceLocator),
        typeof(Core.GameServiceRegistrar),
        typeof(Core.Audio.AudioManager),
        typeof(Core.PoolHelper),
        typeof(Core.PoolConst),
        typeof(Core.Lua.LuaPoolHelper),
        typeof(Core.AssetLoader),
    };

    [CSharpCallLua]
    public static List<Type> CSharpCallLua = new List<Type>()
    {
        typeof(Core.SharedModel.LuaVoidAction),
        typeof(Core.SharedModel.LuaSkillQueryAction),
        typeof(Core.SharedModel.LuaElementAction),
        typeof(Core.SharedModel.LuaBoolElementAction),
        typeof(Core.SharedModel.LuaSceneAction),
        typeof(Core.SharedModel.LuaBoolAction),
        typeof(Core.SharedModel.LuaAttackAction),
    };

    [GCOptimize]
    public static List<Type> GCOptimize = new List<Type>()
    {
        typeof(Core.SharedModel.RunePower),
        typeof(Core.SharedModel.DamageResult),
        typeof(Core.SharedModel.PlayerDamageResult),
    };
}
