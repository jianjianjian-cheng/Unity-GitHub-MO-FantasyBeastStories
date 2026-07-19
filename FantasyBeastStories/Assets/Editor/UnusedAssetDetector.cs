using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class UnusedAssetDetector
{
    [MenuItem("Tools/检测未使用资源")]
    public static void DetectUnusedAssets()
    {
        Debug.Log("[UnusedAssetDetector] 开始检测...");

        // 1. 收集所有"根"资产（入口点）
        var roots = new HashSet<string>();

        // 1a. Build Settings 中的场景
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled && !string.IsNullOrEmpty(scene.path) && File.Exists(scene.path))
                roots.Add(scene.path);
        }

        // 1b. 所有场景文件（即使不在 Build Settings，也可能通过代码加载）
        var allSceneGuids = AssetDatabase.FindAssets("t:Scene");
        foreach (var guid in allSceneGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
                roots.Add(path);
        }

        // 1c. Resources 文件夹中的所有资产
        var allAssetPaths = AssetDatabase.GetAllAssetPaths();
        foreach (var path in allAssetPaths)
        {
            if (path.Contains("/Resources/") || path.Contains("/Resources\\") || path.StartsWith("Assets/Resources/"))
                roots.Add(path);
        }

        // 1d. StreamingAssets 中的资产
        foreach (var path in allAssetPaths)
        {
            if (path.StartsWith("Assets/StreamingAssets/"))
                roots.Add(path);
        }

        // 1e. 所有脚本（编译需要）
        foreach (var path in allAssetPaths)
        {
            if (path.EndsWith(".cs") || path.EndsWith(".asmdef") || path.EndsWith(".asmref") || path.EndsWith(".rsp"))
                roots.Add(path);
        }

        // 1f. Editor 文件夹中的资产
        foreach (var path in allAssetPaths)
        {
            if (path.Contains("/Editor/") && path.StartsWith("Assets/"))
                roots.Add(path);
        }

        // 1g. Shader 文件（可能通过 Shader.Find 引用）
        foreach (var path in allAssetPaths)
        {
            if (path.EndsWith(".shader") || path.EndsWith(".shadersubgraph") || path.EndsWith(".compute") || path.EndsWith(".hlsl") || path.EndsWith(".cginc"))
                roots.Add(path);
        }

        // 1h. Preset 和 Settings 资产
        foreach (var path in allAssetPaths)
        {
            if (path.EndsWith(".preset") || path.StartsWith("ProjectSettings/"))
                roots.Add(path);
        }

        // 1i. AnimatorController 和 AnimationClip（可能通过代码加载）
        foreach (var path in allAssetPaths)
        {
            if (path.EndsWith(".controller") || path.EndsWith(".anim"))
                roots.Add(path);
        }

        Debug.Log($"[UnusedAssetDetector] 根资产数量: {roots.Count}");

        // 2. 从根资产递归收集所有依赖
        var reachable = new HashSet<string>();
        var toProcess = new Queue<string>(roots);

        while (toProcess.Count > 0)
        {
            var current = toProcess.Dequeue();
            if (reachable.Contains(current))
                continue;
            reachable.Add(current);

            try
            {
                var deps = AssetDatabase.GetDependencies(current, false);
                foreach (var dep in deps)
                {
                    if (!reachable.Contains(dep))
                        toProcess.Enqueue(dep);
                }
            }
            catch (Exception e)
            {
                // 忽略错误，继续处理
                Debug.LogWarning($"[UnusedAssetDetector] 获取依赖失败: {current} - {e.Message}");
            }
        }

        Debug.Log($"[UnusedAssetDetector] 可达资产数量: {reachable.Count}");

        // 3. 收集所有项目资产（排除文件夹、meta 文件、PackageCache）
        var allAssets = new HashSet<string>();
        foreach (var path in allAssetPaths)
        {
            if (path.StartsWith("Packages/") || path.StartsWith("Library/"))
                continue;
            if (path.EndsWith(".meta"))
                continue;
            if (AssetDatabase.IsValidFolder(path))
                continue;
            allAssets.Add(path);
        }

        Debug.Log($"[UnusedAssetDetector] 项目总资产数量: {allAssets.Count}");

        // 4. 找出未使用的资产
        var unused = allAssets.Except(reachable).OrderBy(p => p).ToList();

        // 5. 按类型分类统计
        var byType = new Dictionary<string, List<string>>();
        foreach (var path in unused)
        {
            var ext = Path.GetExtension(path).ToLower();
            if (string.IsNullOrEmpty(ext))
                ext = "(无扩展名)";
            if (!byType.ContainsKey(ext))
                byType[ext] = new List<string>();
            byType[ext].Add(path);
        }

        // 6. 生成报告
        var report = new StringBuilder();
        report.AppendLine("========== 未使用资源检测报告 ==========");
        report.AppendLine($"检测时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"项目总资产: {allAssets.Count}");
        report.AppendLine($"可达资产: {reachable.Count}");
        report.AppendLine($"未使用资产: {unused.Count}");
        report.AppendLine();
        report.AppendLine("===== 按类型统计 =====");
        foreach (var kv in byType.OrderByDescending(k => k.Value.Count))
        {
            report.AppendLine($"  {kv.Key}: {kv.Value.Count} 个");
        }
        report.AppendLine();
        report.AppendLine("===== 未使用资源列表 =====");
        foreach (var path in unused)
        {
            var size = GetAssetSize(path);
            report.AppendLine($"  {path}  ({size})");
        }

        // 7. 估算总大小
        long totalSize = 0;
        foreach (var path in unused)
        {
            totalSize += GetAssetSizeBytes(path);
        }
        report.AppendLine();
        report.AppendLine($"===== 未使用资源总大小: {FormatSize(totalSize)} =====");

        // 8. 写入文件
        var reportPath = Application.dataPath.Replace("/Assets", "") + "/UnusedAssetReport.txt";
        File.WriteAllText(reportPath, report.ToString(), Encoding.UTF8);

        Debug.Log($"[UnusedAssetDetector] 检测完成！未使用资源: {unused.Count} 个，总大小: {FormatSize(totalSize)}");
        Debug.Log($"[UnusedAssetDetector] 报告已保存到: {reportPath}");

        // 9. 在控制台输出前 50 个
        Debug.Log("[UnusedAssetDetector] 前 50 个未使用资源:");
        for (int i = 0; i < Math.Min(50, unused.Count); i++)
        {
            Debug.Log($"  [{i + 1}] {unused[i]}");
        }
    }

    private static long GetAssetSizeBytes(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
                return new FileInfo(fullPath).Length;
        }
        catch { }
        return 0;
    }

    private static string GetAssetSize(string path)
    {
        return FormatSize(GetAssetSizeBytes(path));
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }
}
