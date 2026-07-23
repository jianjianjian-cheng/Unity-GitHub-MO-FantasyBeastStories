using System.IO;
using UnityEngine;

namespace Core.Save
{
    /// <summary>
    /// 文件数据处理器 —— 负责将字符串数据写入硬盘 / 从硬盘读取。
    ///
    /// 职责：
    /// - 管理存档文件的目录路径
    /// - 提供 Save / Load / Delete / HasSave 接口
    /// - 支持可选的 AES 加密（由调用方传入已加密/解密的字符串）
    ///
    /// 使用方式：
    ///   var handler = new FileDataHandler(Application.persistentDataPath);
    ///   handler.Save("save_0", jsonString, useEncryption: false);
    ///   string json = handler.Load("save_0", useEncryption: false);
    ///
    /// 设计说明：
    /// - 不继承 MonoBehaviour，不挂载到场景中，由 SaveManager 创建并持有。
    /// - 不关心数据内容，只做文件 IO。
    /// - 所有操作 try-catch 保护，不抛出异常。
    /// </summary>
    public class FileDataHandler
    {
        private readonly string saveDirectory;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="basePath">根路径，通常传入 Application.persistentDataPath</param>
        public FileDataHandler(string basePath)
        {
            saveDirectory = Path.Combine(basePath, "saves");

            // 确保存档目录存在
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
                Debug.Log($"[FileDataHandler] 创建存档目录: {saveDirectory}");
            }
        }

        /// <summary>
        /// 保存数据到指定文件
        /// </summary>
        /// <param name="fileName">文件名（不含路径），如 "save_0"</param>
        /// <param name="data">要写入的字符串数据（JSON）</param>
        /// <param name="useEncryption">是否加密后再写入</param>
        public void Save(string fileName, string data, bool useEncryption)
        {
            string fullPath = GetFullPath(fileName);

            try
            {
                // 创建目录（如果被误删了）
                string directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 处理加密
                string outputData = data;
                if (useEncryption)
                {
                    outputData = EncryptionHelper.Encrypt(data);
                }

                // 写入文件
                File.WriteAllText(fullPath, outputData);

                Debug.Log($"[FileDataHandler] 保存成功 → {fullPath} (加密={useEncryption})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FileDataHandler] 保存失败: {fullPath}\n错误: {e.Message}");
            }
        }

        /// <summary>
        /// 从指定文件加载数据
        /// </summary>
        /// <param name="fileName">文件名（不含路径），如 "save_0"</param>
        /// <param name="useEncryption">文件是否加密过</param>
        /// <returns>原始字符串数据；如果文件不存在或出错则返回 null</returns>
        public string Load(string fileName, bool useEncryption)
        {
            string fullPath = GetFullPath(fileName);

            if (!File.Exists(fullPath))
            {
                Debug.Log($"[FileDataHandler] 存档文件不存在: {fullPath}");
                return null;
            }

            try
            {
                string rawData = File.ReadAllText(fullPath);

                // 处理解密
                string result = rawData;
                if (useEncryption)
                {
                    result = EncryptionHelper.Decrypt(rawData);
                }

                Debug.Log($"[FileDataHandler] 加载成功 ← {fullPath} (加密={useEncryption})");
                return result;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FileDataHandler] 加载失败: {fullPath}\n错误: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 删除指定存档文件
        /// </summary>
        /// <param name="fileName">文件名（不含路径），如 "save_0"</param>
        public void Delete(string fileName)
        {
            string fullPath = GetFullPath(fileName);

            if (!File.Exists(fullPath))
            {
                Debug.Log($"[FileDataHandler] 删除失败，文件不存在: {fullPath}");
                return;
            }

            try
            {
                File.Delete(fullPath);
                Debug.Log($"[FileDataHandler] 删除成功: {fullPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FileDataHandler] 删除失败: {fullPath}\n错误: {e.Message}");
            }
        }

        /// <summary>
        /// 检查指定存档文件是否存在
        /// </summary>
        /// <param name="fileName">文件名（不含路径），如 "save_0"</param>
        /// <returns>是否存在</returns>
        public bool HasSave(string fileName)
        {
            string fullPath = GetFullPath(fileName);
            return File.Exists(fullPath);
        }

        /// <summary>
        /// 获取存档文件的完整路径
        /// </summary>
        /// <param name="fileName">文件名（不含路径）</param>
        /// <returns>完整路径，如 "C:/.../saves/save_0.json"</returns>
        private string GetFullPath(string fileName)
        {
            // 确保文件名有 .json 扩展名
            if (!fileName.EndsWith(".json"))
            {
                fileName += ".json";
            }
            return Path.Combine(saveDirectory, fileName);
        }
    }
}