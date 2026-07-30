using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Core.Editor
{
    /// <summary>
    /// 自定义导入器：将 .lua 文件导入为 TextAsset，使 Addressables 和 xLua 能直接加载。
    /// </summary>
    [ScriptedImporter(1, "lua")]
    public class LuaScriptedImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            string text = File.ReadAllText(ctx.assetPath);
            var asset = new TextAsset(text);
            ctx.AddObjectToAsset("main", asset);
            ctx.SetMainObject(asset);
        }
    }
}
