using Core;
using UnityEngine;

namespace Core.Lua
{
    /// <summary>
    /// Lua 可调用的对象池注册辅助方法。
    /// 替代原 WizardBoy/BingNv 子类中通过事件通道注册池的复杂逻辑。
    /// </summary>
    public static class LuaPoolHelper
    {
        /// <summary>
        /// 加载 Addressables 预制体并注册到对象池。
        /// 由 Lua 的 OnSwitchElement / OnUnlockElement 调用。
        /// </summary>
        public static void RegisterPool(string poolName, string addressablePath, int count)
        {
            if (string.IsNullOrEmpty(poolName) || string.IsNullOrEmpty(addressablePath))
            {
                Debug.LogWarning($"[LuaPoolHelper] 参数无效: poolName={poolName}, path={addressablePath}");
                return;
            }

            var prefab = AssetLoader.TryLoadAsset<GameObject>(addressablePath);
            if (prefab == null)
            {
                Debug.LogWarning($"[LuaPoolHelper] 无法加载预制体: {addressablePath}");
                return;
            }

            var container = EventChannelLocator.MainContainer;
            if (container == null || container.poolOperationChannel == null)
            {
                Debug.LogWarning("[LuaPoolHelper] EventChannel 未初始化");
                return;
            }

            container.poolOperationChannel.Raise(
                PoolOperationData.CreateAddMultiple(poolName, prefab, count));
            Debug.Log($"[LuaPoolHelper] 注册对象池: {poolName} x{count} (from {addressablePath})");
        }

        /// <summary>
        /// 检查池是否存在，不存在则创建。
        /// 由 Lua 的 OnInitElementPool（网络同步）调用。
        /// </summary>
        public static void EnsurePoolCreated(string poolName, string addressablePath, int count)
        {
            if (string.IsNullOrEmpty(poolName)) return;

            int currentCount = 0;
            EventChannelLocator.MainContainer?.poolOperationChannel?.Raise(
                PoolOperationData.CreateGetPoolCount(poolName, (c) => currentCount = c));

            if (currentCount == 0)
            {
                RegisterPool(poolName, addressablePath, count);
            }
        }
    }
}