using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// Lua 脚本安全校验：MD5 + RSA 签名。
/// Phase 7 接入 LuaEnvManager.CustomLoader，加载前验证完整性。
/// </summary>
public static class LuaSecurity
{
    /// <summary>RSA 公钥（随包内置，不可热更）。Phase 7 时替换为实际公钥。</summary>
    private static string _publicKey = string.Empty;

    /// <summary>是否启用安全校验</summary>
    public static bool Enabled { get; set; } = false;

    /// <summary>设置 RSA 公钥</summary>
    public static void SetPublicKey(string key)
    {
        _publicKey = key;
    }

    /// <summary>
    /// 校验 Lua 文件内容（MD5 比对）。
    /// 当前为基础实现，Phase 7 扩展为 RSA 签名校验。
    /// </summary>
    public static bool VerifyContent(byte[] data, string expectedMd5)
    {
        if (string.IsNullOrEmpty(expectedMd5)) return true;

        string actualMd5 = ComputeMd5(data);
        bool valid = string.Equals(actualMd5, expectedMd5, StringComparison.OrdinalIgnoreCase);
        if (!valid)
            Debug.LogWarning($"[LuaSecurity] MD5 校验失败: expected={expectedMd5}, actual={actualMd5}");
        return valid;
    }

    /// <summary>计算 MD5 哈希</summary>
    public static string ComputeMd5(byte[] data)
    {
        using (var md5 = MD5.Create())
        {
            var hash = md5.ComputeHash(data);
            var sb = new StringBuilder();
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    /// <summary>
    /// RSA 签名校验（Phase 7 实现）
    /// </summary>
    public static bool VerifyManifestSignature(byte[] manifestData, byte[] signature)
    {
        if (string.IsNullOrEmpty(_publicKey) || signature == null)
            return false;

        try
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(_publicKey);
                return rsa.VerifyData(manifestData, new SHA256Managed(), signature);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LuaSecurity] 签名校验异常: {e.Message}");
            return false;
        }
    }
}