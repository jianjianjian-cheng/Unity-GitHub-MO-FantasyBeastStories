using System.Collections.Generic;
using UnityEngine;

namespace Controllers.Combat
{
    /// <summary>
    /// 投射物模板注册表：ID → Addressable 路径映射，支持运行时动态注册。
    /// 新角色热更时由 Lua OnStart 调用 Register 注册自己的投射物。
    /// </summary>
    public static class ProjectileTemplateRegistry
    {
        private static readonly Dictionary<int, string> _templates = new Dictionary<int, string>();

        /// <summary>注册投射物模板（可由 Lua 热更脚本调用）</summary>
        public static void Register(int id, string addressablePath)
        {
            _templates[id] = addressablePath;
            Debug.Log($"[ProjectileTemplateRegistry] 注册模板: ID={id}, Path={addressablePath}");
        }

        /// <summary>获取投射物的 Addressable 路径</summary>
        public static string GetName(int id)
        {
            return _templates.TryGetValue(id, out var path) ? path : null;
        }

        /// <summary>检查模板是否已注册</summary>
        public static bool Contains(int id)
        {
            return _templates.ContainsKey(id);
        }
    }
}
