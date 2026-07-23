using Core.Contracts;
using Core.Network;
using UnityEngine;

namespace Controllers.Network
{
    /// <summary>
    /// 早期游戏服务注册器
    /// 用途：在 Launcher 场景对象加载之前，先注册 ObjectPoolService 和 GameActionService
    /// 这样 LobbyCanvas 等组件在 Start() 中就能直接使用这些服务
    /// 
    /// Launcher 的 Awake() 会覆盖本注册器注册的服务实例，因此
    /// 在 Launcher 加载后，实际调用会直接走 Launcher 自身的方法
    /// </summary>
    public static class GameServiceRegistrar
    {
        private static bool _isRegistered = false;

        /// <summary>
        /// 注册早期的轻量级服务实现
        /// 由 InfrastructureRegistrar 在游戏启动时调用
        /// </summary>
        public static void EnsureRegistered()
        {
            if (_isRegistered)
                return;
            _isRegistered = true;

            NetworkServiceLocator.RegisterObjectPoolService(new EarlyObjectPoolService());
            NetworkServiceLocator.RegisterGameActionService(new EarlyGameActionService());

            Debug.Log("[GameServiceRegistrar] 初始服务注册完成（Launcher 加载后会被覆盖）");
        }

        /// <summary>
        /// 轻量级 IObjectPoolService 实现
        /// GetInactiveObjectByName — 自包含实现，不依赖 Launcher
        /// ReturnToLobby — 委托给 Launcher.instance（此时 Launcher 通常已加载）
        /// </summary>
        private class EarlyObjectPoolService : IObjectPoolService
        {
            public GameObject GetInactiveObjectByName(string objectName)
            {
                var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                return System.Array.Find(
                    allObjects,
                    obj =>
                        obj != null
                        && obj.name == objectName
                        && !obj.activeInHierarchy
                        && obj.scene.IsValid()
                );
            }

            public void ReturnToLobby()
            {
                if (Launcher.instance != null)
                {
                    Launcher.instance.ReturnToLobby();
                }
                else
                {
                    Debug.LogWarning("[EarlyObjectPoolService] Launcher 尚未加载，无法返回大厅");
                }
            }
        }

        /// <summary>
        /// 轻量级 IGameActionService 实现
        /// 所有操作委托给 Launcher.instance
        /// </summary>
        private class EarlyGameActionService : IGameActionService
        {
            public void QuitToMainMenu()
            {
                if (Launcher.instance != null)
                {
                    Launcher.instance.QuitToMainMenu();
                }
                else
                {
                    Debug.LogWarning("[EarlyGameActionService] Launcher 尚未加载，无法退出到主菜单");
                }
            }

            public void SetLocalReady(bool ready)
            {
                if (Launcher.instance != null)
                {
                    Launcher.instance.SetLocalReady(ready);
                }
                else
                {
                    Debug.LogWarning("[EarlyGameActionService] Launcher 尚未加载，无法设置准备状态");
                }
            }
        }
    }
}