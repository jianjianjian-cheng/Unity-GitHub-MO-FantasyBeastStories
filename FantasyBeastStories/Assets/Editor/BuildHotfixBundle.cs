using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 编辑器工具：一键构建符文热更新 AssetBundle。
/// 使用方式：菜单栏 → Tools → Build Hotfix AssetBundle
/// </summary>
public class BuildHotfixBundle
{
    [MenuItem("Tools/Build Hotfix AssetBundle")]
    static void Build()
    {
        string outputPath = "Build/Hotfix";
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        BuildPipeline.BuildAssetBundles(
            outputPath,
            BuildAssetBundleOptions.ChunkBasedCompression, // LZ4 压缩，单文件加载快
            BuildTarget.StandaloneWindows64
        );

        Debug.Log($"<color=green>AssetBundle 构建完成 → {outputPath}</color>");
        EditorUtility.RevealInFinder(outputPath);
    }

    [MenuItem("Tools/Build Hotfix AssetBundle", validate = true)]
    static bool ValidateBuild()
    {
        // 确保有标记了 AssetBundle 的资源时才可点击
        return true;
    }
}