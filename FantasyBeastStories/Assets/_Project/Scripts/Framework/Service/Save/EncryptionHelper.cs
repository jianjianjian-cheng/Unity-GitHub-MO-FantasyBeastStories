using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Core.Save
{
    /// <summary>
    /// AES 加密工具 —— 保护存档文件不被玩家直接修改。
    ///
    /// 使用方式：
    ///   string encrypted = EncryptionHelper.Encrypt(plainText);
    ///   string decrypted = EncryptionHelper.Decrypt(encrypted);
    ///
    /// 设计说明：
    /// - 对称加密，加密和解密使用同一个密码
    /// - 密码写死在代码中（对新手项目足够，反作弊需求高时可改用动态密钥）
    /// - 开发阶段 SaveManager.useEncryption = false 可跳过加密，方便调试
    /// - 发布前改为 true
    /// </summary>
    public static class EncryptionHelper
    {
        // 加密密码（发布前可改为更复杂的字符串）
        private const string PASSWORD = "FantasyBeast2026!@#";

        // 盐值（固定，增加破解难度）
        private static readonly byte[] SALT = new byte[] { 0x5A, 0x6B, 0x7C, 0x8D, 0x9E, 0xAF, 0xB0, 0xC1 };

        // 迭代次数（越高越安全，但越慢）
        private const int ITERATIONS = 10000;

        // 密钥长度 256 位
        private const int KEY_SIZE = 32;
        // IV 长度 128 位
        private const int IV_SIZE = 16;

        /// <summary>
        /// 加密明文
        /// </summary>
        /// <param name="plainText">明文字符串</param>
        /// <returns>Base64 编码的密文</returns>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            using (var deriveBytes = new Rfc2898DeriveBytes(PASSWORD, SALT, ITERATIONS, HashAlgorithmName.SHA256))
            {
                byte[] key = deriveBytes.GetBytes(KEY_SIZE);
                byte[] iv = deriveBytes.GetBytes(IV_SIZE);

                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var ms = new MemoryStream())
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
        }

        /// <summary>
        /// 解密密文
        /// </summary>
        /// <param name="cipherText">Base64 编码的密文</param>
        /// <returns>明文字符串</returns>
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);

                using (var deriveBytes = new Rfc2898DeriveBytes(PASSWORD, SALT, ITERATIONS, HashAlgorithmName.SHA256))
                {
                    byte[] key = deriveBytes.GetBytes(KEY_SIZE);
                    byte[] iv = deriveBytes.GetBytes(IV_SIZE);

                    using (var aes = Aes.Create())
                    {
                        aes.Key = key;
                        aes.IV = iv;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;

                        using (var ms = new MemoryStream())
                        using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.FlushFinalBlock();
                            return Encoding.UTF8.GetString(ms.ToArray());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[EncryptionHelper] 解密失败: {ex.Message}");
                return null;
            }
        }
    }
}