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

            // 1. 初始化 Addressables
            Debug.Log("[热更] 初始化 Addressables...");
            var initHandle = Addressables.InitializeAsync();
            yield return initHandle;

            // 开发模式：跳过远程检查
            if (devMode)
            {
                Debug.Log("[热更] 开发模式：跳过远程检查，使用本地 catalog");
                State = UpdateState.Complete;
                IsUpdateComplete = true;
                DownloadProgress = 1f;
                yield break;
            }

            // 2. 检查 catalog 是否有更新
            State = UpdateState.Checking;
            DownloadStartTime = Time.time;
            Debug.Log("[热更] 检查 catalog 更新...");
            var checkHandle = Addressables.CheckForCatalogUpdates(false);
            yield return checkHandle;

            bool needUpdate = false;

            if (checkHandle.Status == AsyncOperationStatus.Succeeded && checkHandle.Result.Count > 0)
            {
                needUpdate = true;
                HasRemoteUpdate = true;
                Debug.Log($"[热更] 检测到 {checkHandle.Result.Count} 个 catalog 需要更新");

                // 3. 更新 catalog
                var updateCatalogHandle = Addressables.UpdateCatalogs(checkHandle.Result, false);
                yield return updateCatalogHandle;
                Addressables.Release(updateCatalogHandle);
                Debug.Log("[热更] Catalog 更新完成");
            }
            else
            {
                Debug.Log("[热更] Catalog 无需更新");
            }
            Addressables.Release(checkHandle);

            if (!needUpdate)
            {
                State = UpdateState.Complete;
                IsUpdateComplete = true;
                DownloadProgress = 1f;
                Debug.Log("[热更] 已是最新版本，跳过下载");
                yield break;
            }

            // 4. 计算需要下载的大小（通过 Label 预下载）
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

            // 5. 下载更新的 bundle
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
    }
}
