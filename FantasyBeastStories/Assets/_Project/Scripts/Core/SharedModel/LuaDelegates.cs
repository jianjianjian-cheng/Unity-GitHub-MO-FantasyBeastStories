using UnityEngine;

namespace Core.SharedModel
{
    /// <summary>
    /// C# → Lua 委托类型定义
    /// 用于 XLua 的 [CSharpCallLua] 代码生成，替代 LuaFunction.Call()
    /// </summary>
    public delegate void LuaVoidAction(Object player);
    public delegate void LuaSkillQueryAction(Object player, SkillQueryData data);
    public delegate void LuaElementAction(Object player, int elementInt);
    public delegate bool LuaBoolElementAction(Object player, int elementInt);
    public delegate void LuaSceneAction(Object player, int sceneIndex);
    public delegate bool LuaBoolAction(Object range);
    public delegate void LuaAttackAction(Object range, Object target);
}
