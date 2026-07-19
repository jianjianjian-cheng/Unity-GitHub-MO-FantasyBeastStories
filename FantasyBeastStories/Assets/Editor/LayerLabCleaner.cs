using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class LayerLabCleaner
{
    const string LAYER_LAB_ROOT = "Assets/UI/Layer Lab";

    [MenuItem("Tools/删除 Layer Lab 未使用资源")]
    public static void DeleteUnusedLayerLabAssets()
    {
        Debug.Log("[LayerLabCleaner] 开始分析并删除未使用的 Layer Lab 资源...");

        // 1. 收集 Layer Lab 下所有非脚本资产
        var layerLabAssets = new List<string>();
        var allGuids = AssetDatabase.FindAssets("", new[] { LAYER_LAB_ROOT });
        foreach (var guid in allGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            if (path.EndsWith(".meta")) continue;
            if (AssetDatabase.IsValidFolder(path)) continue;
            if (path.EndsWith(".cs")) continue; // 保留脚本
            layerLabAssets.Add(path);
        }
        Debug.Log($"[LayerLabCleaner] Layer Lab 下非脚本资产总数: {layerLabAssets.Count}");

        // 2. 收集所有场景
        var scenePaths = new List<string>();
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (!string.IsNullOrEmpty(s.path) && File.Exists(s.path))
                scenePaths.Add(s.path);
        }
        var allSceneGuids = AssetDatabase.FindAssets("t:Scene");
        foreach (var guid in allSceneGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!scenePaths.Contains(path) && File.Exists(path))
                scenePaths.Add(path);
        }

        // 3. 收集所有 Prefab 和 ScriptableObject
        var allPrefabs = AssetDatabase.FindAssets("t:Prefab")
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Where(p => File.Exists(p) && !p.StartsWith(LAYER_LAB_ROOT))
            .ToList();

        var allSOs = AssetDatabase.FindAssets("t:ScriptableObject")
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Where(p => File.Exists(p) && !p.StartsWith(LAYER_LAB_ROOT))
            .ToList();

        // 4. 收集所有其他资产（排除 Layer Lab 自身）
        var allOtherAssets = AssetDatabase.GetAllAssetPaths()
            .Where(p => File.Exists(p)
                && !p.EndsWith(".meta")
                && !p.StartsWith(LAYER_LAB_ROOT)
                && !p.StartsWith("Packages/")
                && !p.StartsWith("Library/")
                && !p.EndsWith(".cs"))
            .ToList();

        // 5. 计算被引用的 Layer Lab 资产
        var usedAssets = new HashSet<string>();

        foreach (var scenePath in scenePaths)
        {
            foreach (var dep in AssetDatabase.GetDependencies(scenePath, false))
            {
                if (dep.StartsWith(LAYER_LAB_ROOT) && !dep.EndsWith(".cs"))
                    usedAssets.Add(dep);
            }
        }

        foreach (var prefabPath in allPrefabs)
        {
            foreach (var dep in AssetDatabase.GetDependencies(prefabPath, false))
            {
                if (dep.StartsWith(LAYER_LAB_ROOT) && !dep.EndsWith(".cs"))
                    usedAssets.Add(dep);
            }
        }

        foreach (var soPath in allSOs)
        {
            foreach (var dep in AssetDatabase.GetDependencies(soPath, false))
            {
                if (dep.StartsWith(LAYER_LAB_ROOT) && !dep.EndsWith(".cs"))
                    usedAssets.Add(dep);
            }
        }

        foreach (var otherPath in allOtherAssets)
        {
            foreach (var dep in AssetDatabase.GetDependencies(otherPath, false))
            {
                if (dep.StartsWith(LAYER_LAB_ROOT) && !dep.EndsWith(".cs"))
                    usedAssets.Add(dep);
            }
        }

        // 6. 找出未使用的
        var unusedAssets = layerLabAssets.Where(a => !usedAssets.Contains(a)).OrderBy(p => p).ToList();
        Debug.Log($"[LayerLabCleaner] 已使用: {usedAssets.Count}, 待删除: {unusedAssets.Count}");

        // 7. 删除未使用资产
        int deleted = 0;
        long freedBytes = 0;
        var deleteErrors = new List<string>();

        foreach (var path in unusedAssets)
        {
            try
            {
                // 记录文件大小
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                    freedBytes += new FileInfo(fullPath).Length;

                // 删除资产（AssetDatabase.DeleteAsset 会同时删除 .meta）
                if (AssetDatabase.DeleteAsset(path))
                {
                    deleted++;
                }
                else
                {
                    deleteErrors.Add($"删除失败: {path}");
                }
            }
            catch (Exception e)
            {
                deleteErrors.Add($"异常: {path} - {e.Message}");
            }
        }

        // 8. 清理空文件夹
        var cleanedFolders = new List<string>();
        CleanEmptyFolders(LAYER_LAB_ROOT, cleanedFolders);

        // 9. 保存
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 10. 输出结果
        var msg = new StringBuilder();
        msg.AppendLine($"[LayerLabCleaner] 删除完成！");
        msg.AppendLine($"  删除资产数: {deleted}");
        msg.AppendLine($"  释放空间: {FormatSize(freedBytes)}");
        msg.AppendLine($"  清理空文件夹: {cleanedFolders.Count}");
        if (cleanedFolders.Count > 0)
        {
            msg.AppendLine("  空文件夹列表:");
            foreach (var f in cleanedFolders)
                msg.AppendLine($"    - {f}");
        }
        if (deleteErrors.Count > 0)
        {
            msg.AppendLine($"  删除错误: {deleteErrors.Count}");
            foreach (var e in deleteErrors)
                msg.AppendLine($"    {e}");
        }

        Debug.Log(msg.ToString());
    }

    static void CleanEmptyFolders(string rootPath, List<string> cleaned)
    {
        // 递归先处理子文件夹
        var subFolders = AssetDatabase.GetSubFolders(rootPath);
        foreach (var sub in subFolders)
        {
            CleanEmptyFolders(sub, cleaned);
        }

        // 检查当前文件夹是否为空
        var children = AssetDatabase.FindAssets("", new[] { rootPath });
        if (children.Length == 0)
        {
            if (AssetDatabase.DeleteAsset(rootPath))
            {
                cleaned.Add(rootPath);
            }
        }
    }

    static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }
}
