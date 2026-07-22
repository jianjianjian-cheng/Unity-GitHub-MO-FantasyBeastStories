using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Core
{
    /// <summary>
    /// Addressables 热更新管理器。
    /// 游戏启动时检查远程 catalog 是否有更新，有则下载变化的 bundle。
    /// 需挂在启动场景的 GameObject 上，且应在 GameManager/LuaEnvManager 之前执行。
    /// </summary>
    public class AddressablesUpdater : MonoBehaviour
    {
        public enum UpdateState { Idle, Checking, Downloading, Complete, Failed }

        public static AddressablesUpdater Instance { get; private set; }

        /// <summary>热更新是否已完成</summary>
        public static bool IsUpdateComplete { get; private set; }

        /// <summary>热更下载进度 (0~1)</summary>
        public static float DownloadProgress { get; private set; }

        /// <summary>是否检测到了远程更新</summary>
        public static bool HasRemoteUpdate { get; private set; }

        /// <summary>当前热更状态</summary>
        public static UpdateState State { get; private set; }

        /// <summary>需要下载的总字节数</summary>
        public static long TotalDownloadBytes { get; private set; }

        /// <summary>下载开始的时间（用于 UI 保证最小显示时长）</summary>
        public static float DownloadStartTime { get; private set; }

        [Header("热更配置")]
        [Tooltip("需要预下载的资源 Label（用逗号分隔的 Label 列表，不是 Group 名）")]
        [SerializeField] private List<string> preloadLabels = new List<string>
        {
            "remote_preload"
        };

        [Tooltip("开发模式：跳过远程检查，仅使用本地 catalog（Editor 测试用）")]
        [SerializeField] private bool devMode = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private IEnumerator Start()
        {
            IsUpdateComplete = false;
            DownloadProgress = 0f;
            HasRemoteUpdate = false;
            State = UpdateState.Idle;
            TotalDownloadBytes = 0;

            // 开发模式：跳过远程检查
            if (devMode)
            {
                Debug.Log("[热更] 开发模式：初始化 Addressables...");
                var devInitHandle = Addressables.InitializeAsync();
                yield return devInitHandle;
                Debug.Log("[热更] 开发模式：跳过远程检查，使用本地 catalog");
                State = UpdateState.Complete;
                IsUpdateComplete = true;
                DownloadProgress = 1f;
                yield break;
            }

            // 1. 读取玩家包内置 catalog，计算其 MD5 hash
            State = UpdateState.Checking;
            DownloadStartTime = Time.time;
            Debug.Log("[热更] 检查 catalog 更新...");

            string builtinHash = ComputeBuiltinCatalogHash();
            Debug.Log($"[热更] 内置 catalog hash: {builtinHash}");

            // 2. 下载远程 hash
            string remoteHash = null;
            var remoteHashPath = "https://a.unity.cn/client_api/v1/buckets/9a90add4-6c36-4796-a3ea-9114ea7561dc/release_by_badge/latest/content/catalog_0.1.hash";
            using (var www = UnityEngine.Networking.UnityWebRequest.Get(remoteHashPath))
            {
                www.redirectLimit = 10;
                yield return www.SendWebRequest();
                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    remoteHash = www.downloadHandler.text.Trim();
                    Debug.Log($"[热更] 远程 catalog hash: {remoteHash}");
                }
                else
                {
                    Debug.LogError($"[热更] 获取远程 hash 失败: {www.error}");
                }
            }

            // 3. 对比：远程 hash ≠ 内置 hash → 需要更新
            bool needUpdate = false;
            if (!string.IsNullOrEmpty(remoteHash) && !string.IsNullOrEmpty(builtinHash) && remoteHash != builtinHash)
            {
                needUpdate = true;
                HasRemoteUpdate = true;
                Debug.Log("[热更] 检测到 catalog 需要更新（远程 hash ≠ 内置 hash）");
            }
            else if (string.IsNullOrEmpty(remoteHash))
            {
                Debug.LogWarning("[热更] 无法获取远程 hash，跳过更新");
            }
            else
            {
                Debug.Log("[热更] Catalog 无需更新（远程 hash = 内置 hash）");
            }

            // 4. 初始化 Addressables
            Debug.Log("[热更] 初始化 Addressables...");
            var initHandle = Addressables.InitializeAsync();
            yield return initHandle;

            if (needUpdate)
            {
                // 手动下载远程 catalog 写入缓存，再重新初始化
                Debug.Log("[热更] 正在下载远程 catalog...");
                yield return DownloadRemoteCatalog(remoteHashPath);
            }

            // 打印 catalog 中的资源信息，用于调试
            Debug.Log("[热更] === 当前内存中的 Catalog 资源列表 ===");
            foreach (var locator in UnityEngine.AddressableAssets.Addressables.ResourceLocators)
            {
                Debug.Log($"[热更] Locator: {locator.LocatorId}");
                long count = 0;
                foreach (var key in locator.Keys)
                {
                    var keyStr = key.ToString();
                    if (keyStr.Contains("rune", System.StringComparison.OrdinalIgnoreCase) ||
                        keyStr.Contains("shop", System.StringComparison.OrdinalIgnoreCase) ||
                        keyStr.Contains("RuneExplosion", System.StringComparison.OrdinalIgnoreCase) ||
                        keyStr.Contains("RuneTripleBoost", System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (locator.Locate(key, typeof(UnityEngine.Object), out var locations))
                        {
                            foreach (var loc in locations)
                            {
                                Debug.Log($"[热更]   {keyStr} → {loc.InternalId}");
                            }
                        }
                    }
                    count++;
                }
                Debug.Log($"[热更] Locator 总资源数: {count}");
            }
            Debug.Log("[热更] === Catalog 资源列表结束 ===");

            if (!needUpdate)
            {
                State = UpdateState.Complete;
                IsUpdateComplete = true;
                DownloadProgress = 1f;
                Debug.Log("[热更] 已是最新版本，跳过下载");
                yield break;
            }

            // 5. 计算需要下载的大小（通过 Label 预下载）
            Debug.Log("[热更] 计算下载大小...");
            var sizeHandle = Addressables.GetDownloadSizeAsync(preloadLabels.AsReadOnly());
            yield return sizeHandle;

            TotalDownloadBytes = sizeHandle.Result;
            Addressables.Release(sizeHandle);

            if (TotalDownloadBytes <= 0)
            {
                State = UpdateState.Complete;
                IsUpdateComplete = true;
                DownloadProgress = 1f;
                Debug.Log("[热更] 无需下载新资源");
                yield break;
            }

            float totalMB = TotalDownloadBytes / 1024f / 1024f;
            Debug.Log($"[热更] 需要下载 {totalMB:F1} MB");

            // 6. 下载更新的 bundle
            State = UpdateState.Downloading;
            var downloadHandle = Addressables.DownloadDependenciesAsync(
                preloadLabels.AsReadOnly(),
                Addressables.MergeMode.Union
            );

            while (!downloadHandle.IsDone)
            {
                DownloadProgress = downloadHandle.PercentComplete;
                yield return null;
            }

            if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
            {
                DownloadProgress = 1f;
                State = UpdateState.Complete;
                IsUpdateComplete = true;
                Debug.Log("[热更] 资源下载完成");
            }
            else
            {
                State = UpdateState.Failed;
                Debug.LogError("[热更] 下载失败，使用本地缓存继续");
                IsUpdateComplete = true;
            }

            Addressables.Release(downloadHandle);
        }

        /// <summary>
        /// 手动下载远程 catalog 并写入缓存，绕过 CheckForCatalogUpdates 的缓存机制
        /// </summary>
        private IEnumerator DownloadRemoteCatalog(string hashUrl)
        {
            // 远程 catalog URL = 把 hash 文件 URL 中的 .hash 替换为 .json
            string catalogUrl = hashUrl.Replace(".hash", ".json");

            Debug.Log($"[热更] 下载远程 catalog: {catalogUrl}");
            using (var www = UnityEngine.Networking.UnityWebRequest.Get(catalogUrl))
            {
                www.redirectLimit = 10;
                yield return www.SendWebRequest();

                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    string catalogJson = www.downloadHandler.text;

                    // 写入缓存目录
                    string cacheDir = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "com.unity.addressables");
                    System.IO.Directory.CreateDirectory(cacheDir);

                    System.IO.File.WriteAllText(
                        System.IO.Path.Combine(cacheDir, "catalog_0.1.json"),
                        catalogJson);

                    // 下载并写入 hash 文件
                    using (var hashReq = UnityEngine.Networking.UnityWebRequest.Get(hashUrl))
                    {
                        hashReq.redirectLimit = 10;
                        yield return hashReq.SendWebRequest();
                        if (hashReq.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                        {
                            System.IO.File.WriteAllText(
                                System.IO.Path.Combine(cacheDir, "catalog_0.1.hash"),
                                hashReq.downloadHandler.text);
                        }
                    }

                    Debug.Log("[热更] 远程 catalog 已下载并写入缓存，正在重新初始化...");

                    // 重新初始化 Addressables 加载新 catalog
                    var reinitHandle = Addressables.InitializeAsync();
                    yield return reinitHandle;
                    Debug.Log("[热更] 重新初始化完成，新 catalog 已加载");
                }
                else
                {
                    Debug.LogError($"[热更] 下载远程 catalog 失败: {www.error}");
                }
            }
        }

        /// <summary>
        /// 计算玩家包内置 catalog 的 MD5 hash
        /// catalog_0.1.hash 文件的内容就是 catalog_0.1.json 内容的 MD5
        /// </summary>
        private string ComputeBuiltinCatalogHash()
        {
            try
            {
                // 内置 catalog 路径：StreamingAssets/aa/catalog.json
                string catalogPath = System.IO.Path.Combine(
                    UnityEngine.Application.streamingAssetsPath, "aa", "catalog.json");

                if (!System.IO.File.Exists(catalogPath))
                {
                    Debug.LogWarning($"[热更] 内置 catalog 不存在: {catalogPath}");
                    return null;
                }

                string catalogContent = System.IO.File.ReadAllText(catalogPath);
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(catalogContent);
                    byte[] hash = md5.ComputeHash(bytes);
                    return System.BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[热更] 计算内置 catalog hash 失败: {ex.Message}");
                return null;
            }
        }
    }
}
