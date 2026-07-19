using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LayerLabUsageAnalyzer
{
    const string LAYER_LAB_ROOT = "Assets/UI/Layer Lab";

    [MenuItem("Tools/分析 Layer Lab 资源在各场景中的使用")]
    public static void AnalyzeLayerLabUsage()
    {
        Debug.Log("[LayerLabAnalyzer] 开始分析...");

        // 1. 收集 Layer Lab 下所有资产（排除 .meta、文件夹、.cs 脚本）
        var layerLabAssets = new List<string>();
        var allGuids = AssetDatabase.FindAssets("", new[] { LAYER_LAB_ROOT });
        foreach (var guid in allGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            if (path.EndsWith(".meta")) continue;
            if (AssetDatabase.IsValidFolder(path)) continue;
            if (path.EndsWith(".cs")) continue; // 脚本不删
            layerLabAssets.Add(path);
        }
        layerLabAssets = layerLabAssets.OrderBy(p => p).ToList();
        Debug.Log($"[LayerLabAnalyzer] Layer Lab 下非脚本资产总数: {layerLabAssets.Count}");

        // 2. 收集项目中所有场景
        var scenePaths = new List<string>();
        // Build Settings 中的场景
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (!string.IsNullOrEmpty(s.path) && File.Exists(s.path))
                scenePaths.Add(s.path);
        }
        // 项目中所有其他场景
        var allSceneGuids = AssetDatabase.FindAssets("t:Scene");
        foreach (var guid in allSceneGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!scenePaths.Contains(path) && File.Exists(path))
                scenePaths.Add(path);
        }
        Debug.Log($"[LayerLabAnalyzer] 场景总数: {scenePaths.Count}");

        // 3. 也要检查 Prefab 和 ScriptableObject 是否引用了 Layer Lab 资产
        //    （因为场景可能通过预制体间接引用）
        var allPrefabGuids = AssetDatabase.FindAssets("t:Prefab");
        var allPrefabs = allPrefabGuids.Select(g => AssetDatabase.GUIDToAssetPath(g)).Where(p => File.Exists(p)).ToList();
        Debug.Log($"[LayerLabAnalyzer] Prefab 总数: {allPrefabs.Count}");

        var allAssetGuids = AssetDatabase.FindAssets("t:ScriptableObject");
        var allScriptableObjects = allAssetGuids.Select(g => AssetDatabase.GUIDToAssetPath(g)).Where(p => File.Exists(p)).ToList();
        Debug.Log($"[LayerLabAnalyzer] ScriptableObject 总数: {allScriptableObjects.Count}");

        // 4. 分析每个场景引用了哪些 Layer Lab 资产
        var usedAssets = new HashSet<string>();
        var usageDetail = new Dictionary<string, List<string>>(); // assetPath -> list of scenes/prefabs that use it

        string AddUsage(string asset, string source)
        {
            if (!usageDetail.ContainsKey(asset))
                usageDetail[asset] = new List<string>();
            if (!usageDetail[asset].Contains(source))
                usageDetail[asset].Add(source);
            return asset;
        }

        // 4a. 检查场景
        foreach (var scenePath in scenePaths)
        {
            var deps = AssetDatabase.GetDependencies(scenePath, false);
            foreach (var dep in deps)
            {
                if (dep.StartsWith(LAYER_LAB_ROOT) && !dep.EndsWith(".cs"))
                {
                    usedAssets.Add(dep);
                    AddUsage(dep, $"场景: {Path.GetFileName(scenePath)}");
                }
            }
        }

        // 4b. 检查 Prefab
        foreach (var prefabPath in allPrefabs)
        {
            var deps = AssetDatabase.GetDependencies(prefabPath, false);
            foreach (var dep in deps)
            {
                if (dep.StartsWith(LAYER_LAB_ROOT) && !dep.EndsWith(".cs"))
                {
                    usedAssets.Add(dep);
                    AddUsage(dep, $"Prefab: {Path.GetFileName(prefabPath)}");
                }
            }
        }

        // 4c. 检查 ScriptableObject
        foreach (var soPath in allScriptableObjects)
        {
            var deps = AssetDatabase.GetDependencies(soPath, false);
            foreach (var dep in deps)
            {
                if (dep.StartsWith(LAYER_LAB_ROOT) && !dep.EndsWith(".cs"))
                {
                    usedAssets.Add(dep);
                    AddUsage(dep, $"ScriptableObject: {Path.GetFileName(soPath)}");
                }
            }
        }

        // 4d. 检查所有其他资产（材质、动画控制器等可能引用 Layer Lab 图片）
        var allOtherAssets = AssetDatabase.GetAllAssetPaths()
            .Where(p => File.Exists(p)
                && !p.EndsWith(".meta")
                && !p.StartsWith(LAYER_LAB_ROOT)
                && !p.StartsWith("Packages/")
                && !p.StartsWith("Library/")
                && !p.EndsWith(".cs"))
            .ToList();

        foreach (var otherPath in allOtherAssets)
        {
            var deps = AssetDatabase.GetDependencies(otherPath, false);
            foreach (var dep in deps)
            {
                if (dep.StartsWith(LAYER_LAB_ROOT) && !dep.EndsWith(".cs"))
                {
                    usedAssets.Add(dep);
                    AddUsage(dep, $"其他资产: {otherPath}");
                }
            }
        }

        // 5. 找出未使用的资产
        var unusedAssets = layerLabAssets.Where(a => !usedAssets.Contains(a)).OrderBy(p => p).ToList();
        var usedAssetList = layerLabAssets.Where(a => usedAssets.Contains(a)).OrderBy(p => p).ToList();

        Debug.Log($"[LayerLabAnalyzer] 已使用: {usedAssetList.Count}, 未使用: {unusedAssets.Count}");

        // 6. 生成报告
        var report = new StringBuilder();
        report.AppendLine("========== Layer Lab 资源使用分析报告 ==========");
        report.AppendLine($"分析时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"Layer Lab 资产总数: {layerLabAssets.Count}");
        report.AppendLine($"已使用: {usedAssetList.Count}");
        report.AppendLine($"未使用: {unusedAssets.Count}");
        report.AppendLine();

        // 按类型统计未使用
        var byType = unusedAssets.GroupBy(p => Path.GetExtension(p).ToLower())
            .OrderByDescending(g => g.Count());
        report.AppendLine("===== 未使用资源按类型统计 =====");
        foreach (var g in byType)
        {
            var ext = string.IsNullOrEmpty(g.Key) ? "(无扩展名)" : g.Key;
            report.AppendLine($"  {ext}: {g.Count()} 个");
        }
        report.AppendLine();

        // 按子文件夹统计未使用
        var byFolder = unusedAssets.GroupBy(p =>
        {
            var rel = p.Substring(LAYER_LAB_ROOT.Length + 1);
            var parts = rel.Split('/');
            return parts.Length >= 2 ? string.Join("/", parts.Take(2)) : parts[0];
        }).OrderByDescending(g => g.Count());
        report.AppendLine("===== 未使用资源按子文件夹统计 =====");
        foreach (var g in byFolder)
        {
            report.AppendLine($"  {g.Key}/: {g.Count()} 个");
        }
        report.AppendLine();

        // 已使用资源详情
        report.AppendLine("===== 已使用资源详情 =====");
        foreach (var asset in usedAssetList)
        {
            var sources = usageDetail.ContainsKey(asset) ? string.Join(", ", usageDetail[asset]) : "?";
            report.AppendLine($"  {asset}");
            report.AppendLine($"    ← 引用来源: {sources}");
        }
        report.AppendLine();

        // 未使用资源列表
        long totalUnusedSize = 0;
        report.AppendLine("===== 未使用资源列表 =====");
        foreach (var path in unusedAssets)
        {
            var sizeBytes = GetFileSize(path);
            totalUnusedSize += sizeBytes;
            report.AppendLine($"  {path}  ({FormatSize(sizeBytes)})");
        }
        report.AppendLine();
        report.AppendLine($"===== 未使用资源总大小: {FormatSize(totalUnusedSize)} =====");

        // 写入文件
        var reportPath = Application.dataPath.Replace("/Assets", "") + "/LayerLabUsageReport.txt";
        File.WriteAllText(reportPath, report.ToString(), Encoding.UTF8);

        Debug.Log($"[LayerLabAnalyzer] 分析完成！已使用: {usedAssetList.Count}, 未使用: {unusedAssets.Count} (总大小: {FormatSize(totalUnusedSize)})");
        Debug.Log($"[LayerLabAnalyzer] 报告已保存到: {reportPath}");
    }

    static long GetFileSize(string assetPath)
    {
        try
        {
            var fullPath = Path.GetFullPath(assetPath);
            if (File.Exists(fullPath))
                return new FileInfo(fullPath).Length;
        }
        catch { }
        return 0;
    }

    static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }
}
