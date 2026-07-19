using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class FolderAssetCleaner
{
    // 可配置的目标文件夹（修改此变量即可切换清理目标）
    static string TargetFolder = "Assets/ImportAssets";

    [MenuItem("Tools/清理文件夹未使用资源 (ImportAssets)")]
    public static void CleanImportAssets()
    {
        CleanUnusedAssets("Assets/ImportAssets");
    }

    [MenuItem("Tools/清理文件夹未使用资源 (Fantastic Interior Pack)")]
    public static void CleanFantasticInteriorPack()
    {
        CleanUnusedAssets("Assets/Fantastic Interior Pack");
    }

    [MenuItem("Tools/清理文件夹未使用资源 (PolygonNatureBiomes)")]
    public static void CleanPolygonNatureBiomes()
    {
        CleanUnusedAssets("Assets/PolygonNatureBiomes");
    }

    /// <summary>
    /// 分析并删除指定文件夹下未被任何场景、预制体、ScriptableObject 引用的资产。
    /// 保留 .cs 脚本、.asmdef、.asmref、.rsp、.shader、.shadersubgraph、.compute、.hlsl、.cginc 文件。
    /// </summary>
    public static void CleanUnusedAssets(string rootFolder)
    {
        Debug.Log($"[FolderCleaner] 开始分析: {rootFolder}");

        if (!AssetDatabase.IsValidFolder(rootFolder))
        {
            Debug.LogError($"[FolderCleaner] 文件夹不存在: {rootFolder}");
            return;
        }

        // 1. 收集目标文件夹下所有资产（排除脚本和 shader 源文件）
        var targetAssets = new List<string>();
        var allGuids = AssetDatabase.FindAssets("", new[] { rootFolder });
        foreach (var guid in allGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            if (path.EndsWith(".meta")) continue;
            if (AssetDatabase.IsValidFolder(path)) continue;
            // 保留脚本和 shader 源文件
            if (IsPreservedFile(path)) continue;
            targetAssets.Add(path);
        }
        targetAssets = targetAssets.OrderBy(p => p).ToList();
        Debug.Log($"[FolderCleaner] {rootFolder} 下待评估资产: {targetAssets.Count}");

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

        // 3. 收集所有 Prefab、ScriptableObject（排除目标文件夹自身）
        var allPrefabs = AssetDatabase.FindAssets("t:Prefab")
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Where(p => File.Exists(p) && !p.StartsWith(rootFolder))
            .ToList();

        var allSOs = AssetDatabase.FindAssets("t:ScriptableObject")
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Where(p => File.Exists(p) && !p.StartsWith(rootFolder))
            .ToList();

        // 4. 收集所有其他资产（排除目标文件夹自身）
        var allOtherAssets = AssetDatabase.GetAllAssetPaths()
            .Where(p => File.Exists(p)
                && !p.EndsWith(".meta")
                && !p.StartsWith(rootFolder)
                && !p.StartsWith("Packages/")
                && !p.StartsWith("Library/")
                && !p.EndsWith(".cs"))
            .ToList();

        // 5. 计算被引用的目标资产
        var usedAssets = new HashSet<string>();
        var usageDetail = new Dictionary<string, List<string>>();

        string AddUsage(string asset, string source)
        {
            if (!usedAssets.Contains(asset))
                usedAssets.Add(asset);
            if (!usageDetail.ContainsKey(asset))
                usageDetail[asset] = new List<string>();
            if (!usageDetail[asset].Contains(source))
                usageDetail[asset].Add(source);
            return asset;
        }

        foreach (var scenePath in scenePaths)
        {
            foreach (var dep in AssetDatabase.GetDependencies(scenePath, false))
            {
                if (dep.StartsWith(rootFolder) && !IsPreservedFile(dep))
                    AddUsage(dep, $"场景: {Path.GetFileName(scenePath)}");
            }
        }

        foreach (var prefabPath in allPrefabs)
        {
            foreach (var dep in AssetDatabase.GetDependencies(prefabPath, false))
            {
                if (dep.StartsWith(rootFolder) && !IsPreservedFile(dep))
                    AddUsage(dep, $"Prefab: {Path.GetFileName(prefabPath)}");
            }
        }

        foreach (var soPath in allSOs)
        {
            foreach (var dep in AssetDatabase.GetDependencies(soPath, false))
            {
                if (dep.StartsWith(rootFolder) && !IsPreservedFile(dep))
                    AddUsage(dep, $"SO: {Path.GetFileName(soPath)}");
            }
        }

        foreach (var otherPath in allOtherAssets)
        {
            foreach (var dep in AssetDatabase.GetDependencies(otherPath, false))
            {
                if (dep.StartsWith(rootFolder) && !IsPreservedFile(dep))
                    AddUsage(dep, $"其他: {otherPath}");
            }
        }

        // 6. 找出未使用的
        var unusedAssets = targetAssets.Where(a => !usedAssets.Contains(a)).OrderBy(p => p).ToList();
        var usedList = targetAssets.Where(a => usedAssets.Contains(a)).OrderBy(p => p).ToList();

        Debug.Log($"[FolderCleaner] 已使用: {usedList.Count}, 未使用(待删除): {unusedAssets.Count}");

        // 7. 先生成分析报告（删除前记录）
        var report = new StringBuilder();
        report.AppendLine($"========== {rootFolder} 资源清理报告 ==========");
        report.AppendLine($"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"资产总数: {targetAssets.Count}");
        report.AppendLine($"已使用: {usedList.Count}");
        report.AppendLine($"删除: {unusedAssets.Count}");
        report.AppendLine();

        // 按子文件夹统计
        var byFolder = unusedAssets.GroupBy(p =>
        {
            var rel = p.Substring(rootFolder.Length + 1);
            var idx = rel.IndexOf('/');
            return idx >= 0 ? rel.Substring(0, idx) : rel;
        }).OrderByDescending(g => g.Count());
        report.AppendLine("===== 删除资源按子文件夹统计 =====");
        foreach (var g in byFolder)
            report.AppendLine($"  {g.Key}/: {g.Count()} 个");
        report.AppendLine();

        // 按类型统计
        var byType = unusedAssets.GroupBy(p => Path.GetExtension(p).ToLower())
            .OrderByDescending(g => g.Count());
        report.AppendLine("===== 删除资源按类型统计 =====");
        foreach (var g in byType)
        {
            var ext = string.IsNullOrEmpty(g.Key) ? "(无扩展名)" : g.Key;
            report.AppendLine($"  {ext}: {g.Count()} 个");
        }
        report.AppendLine();

        // 已使用详情
        if (usedList.Count > 0)
        {
            report.AppendLine("===== 保留(已使用)资源详情 =====");
            foreach (var asset in usedList)
            {
                report.AppendLine($"  {asset}");
                if (usageDetail.ContainsKey(asset))
                    report.AppendLine($"    ← {string.Join(", ", usageDetail[asset])}");
            }
            report.AppendLine();
        }

        // 未使用列表
        long totalUnusedSize = 0;
        report.AppendLine("===== 删除资源列表 =====");
        foreach (var path in unusedAssets)
        {
            var sz = GetFileSize(path);
            totalUnusedSize += sz;
            report.AppendLine($"  {path}  ({FormatSize(sz)})");
        }
        report.AppendLine();
        report.AppendLine($"===== 删除资源总大小: {FormatSize(totalUnusedSize)} =====");

        // 写报告
        var reportFileName = rootFolder.Replace("Assets/", "").Replace("/", "_") + "_CleanReport.txt";
        var reportPath = Path.GetFullPath(reportFileName);
        File.WriteAllText(reportPath, report.ToString(), Encoding.UTF8);
        Debug.Log($"[FolderCleaner] 分析报告: {reportPath}");

        // 8. 执行删除
        int deleted = 0;
        long freedBytes = 0;
        var deleteErrors = new List<string>();

        foreach (var path in unusedAssets)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                    freedBytes += new FileInfo(fullPath).Length;

                if (AssetDatabase.DeleteAsset(path))
                    deleted++;
                else
                    deleteErrors.Add($"删除失败: {path}");
            }
            catch (Exception e)
            {
                deleteErrors.Add($"异常: {path} - {e.Message}");
            }
        }

        // 9. 清理空文件夹
        var cleanedFolders = new List<string>();
        CleanEmptyFolders(rootFolder, cleanedFolders);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 10. 输出结果
        var msg = new StringBuilder();
        msg.AppendLine($"[FolderCleaner] 清理完成: {rootFolder}");
        msg.AppendLine($"  删除资产: {deleted}");
        msg.AppendLine($"  释放空间: {FormatSize(freedBytes)}");
        msg.AppendLine($"  清理空文件夹: {cleanedFolders.Count}");
        if (cleanedFolders.Count > 0)
        {
            msg.AppendLine("  空文件夹:");
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

    /// <summary>
    /// 判断是否为应保留的文件（脚本、shader 源文件、程序集定义等）
    /// </summary>
    static bool IsPreservedFile(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        return ext == ".cs"        // C# 脚本
            || ext == ".asmdef"    // 程序集定义
            || ext == ".asmref"    // 程序集引用
            || ext == ".rsp"       // 编译响应文件
            || ext == ".shader"    // Shader 源码
            || ext == ".shadersubgraph" // Shader 子图
            || ext == ".shadergraph"    // Shader Graph
            || ext == ".compute"   // Compute Shader
            || ext == ".hlsl"      // HLSL include
            || ext == ".cginc"     // CG include
            || ext == ".dll";      // DLL（可能有运行时依赖）
    }

    static void CleanEmptyFolders(string rootPath, List<string> cleaned)
    {
        var subFolders = AssetDatabase.GetSubFolders(rootPath);
        foreach (var sub in subFolders)
            CleanEmptyFolders(sub, cleaned);

        var children = AssetDatabase.FindAssets("", new[] { rootPath });
        if (children.Length == 0)
        {
            if (AssetDatabase.DeleteAsset(rootPath))
                cleaned.Add(rootPath);
        }
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
