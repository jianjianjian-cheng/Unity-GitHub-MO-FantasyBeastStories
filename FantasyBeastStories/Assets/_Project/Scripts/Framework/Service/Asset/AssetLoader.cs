using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Core
{
    /// <summary>
    /// 统一资产加载适配层，封装 Addressables 调用。
    /// 提供同步/异步两种接口，替代 Resources.Load。
    /// </summary>
    public static class AssetLoader
    {
        // ========== 异步加载（推荐） ==========

        /// <summary>异步加载资产</summary>
        public static async Task<T> LoadAssetAsync<T>(string key) where T : Object
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            await handle.Task;
            if (handle.Status == AsyncOperationStatus.Succeeded)
                return handle.Result;
            Debug.LogError($"[AssetLoader] 加载失败: {key}");
            return null;
        }

        /// <summary>异步实例化 Prefab</summary>
        public static async Task<GameObject> InstantiateAsync(string key, Vector3 pos = default, Quaternion rot = default)
        {
            var handle = Addressables.InstantiateAsync(key, pos, rot);
            await handle.Task;
            if (handle.Status == AsyncOperationStatus.Succeeded)
                return handle.Result;
            Debug.LogError($"[AssetLoader] 实例化失败: {key}");
            return null;
        }

        // ========== 同步加载（兼容现有代码） ==========

        /// <summary>同步加载资产（阻塞，仅初始化阶段使用）</summary>
        public static T LoadAsset<T>(string key) where T : Object
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            handle.WaitForCompletion();
            if (handle.Status == AsyncOperationStatus.Succeeded)
                return handle.Result;
            Debug.LogError($"[AssetLoader] 加载失败: {key}");
            return null;
        }

        /// <summary>同步尝试加载资产（不打印错误日志，用于 fallback 探测）</summary>
        public static T TryLoadAsset<T>(string key) where T : Object
        {
            if (string.IsNullOrEmpty(key)) return null;

            // 先用 LoadResourceLocationsAsync 检查 key 是否存在（不指定类型，避免类型过滤失败）
            try
            {
                var checkHandle = Addressables.LoadResourceLocationsAsync(key);
                checkHandle.WaitForCompletion();
                if (checkHandle.Status != AsyncOperationStatus.Succeeded || checkHandle.Result == null || checkHandle.Result.Count == 0)
                {
                    Addressables.Release(checkHandle);
                    return null;
                }
                Addressables.Release(checkHandle);
            }
            catch
            {
                return null;
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<T>(key);
                handle.WaitForCompletion();
                if (handle.Status == AsyncOperationStatus.Succeeded)
                    return handle.Result;
                Addressables.Release(handle);
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>检查 Addressable key 是否存在（不抛异常、不打日志）</summary>
        public static bool KeyExists(string key, System.Type type = null)
        {
            if (string.IsNullOrEmpty(key)) return false;
            try
            {
                var handle = type != null
                    ? Addressables.LoadResourceLocationsAsync(key, type)
                    : Addressables.LoadResourceLocationsAsync(key);
                handle.WaitForCompletion();
                bool exists = handle.Status == AsyncOperationStatus.Succeeded
                              && handle.Result != null
                              && handle.Result.Count > 0;
                Addressables.Release(handle);
                return exists;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>同步实例化 Prefab（阻塞，仅初始化阶段使用）</summary>
        public static GameObject Instantiate(string key, Vector3 pos = default, Quaternion rot = default)
        {
            var handle = Addressables.InstantiateAsync(key, pos, rot);
            handle.WaitForCompletion();
            if (handle.Status == AsyncOperationStatus.Succeeded)
                return handle.Result;
            Debug.LogError($"[AssetLoader] 实例化失败: {key}");
            return null;
        }

        // ========== 释放 ==========

        /// <summary>释放资产引用</summary>
        public static void Release<T>(T asset) where T : Object
        {
            if (asset != null)
                Addressables.Release(asset);
        }

        /// <summary>释放通过 InstantiateAsync 实例化的对象</summary>
        public static void ReleaseInstance(GameObject instance)
        {
            if (instance != null)
                Addressables.ReleaseInstance(instance);
        }
    }
}
