namespace Domain.Services
{
    public static class NetworkServiceLocator
    {
        private static INetworkPlayerService _playerService;
        private static INetworkObjectService _objectService;

        public static INetworkPlayerService PlayerService
        {
            get
            {
                if (_playerService == null)
                {
                    UnityEngine.Debug.LogError("[NetworkServiceLocator] INetworkPlayerService 未注册。请在游戏启动时调用 Register() 注册网络服务");
                }
                return _playerService;
            }
        }

        public static INetworkObjectService ObjectService
        {
            get
            {
                if (_objectService == null)
                {
                    UnityEngine.Debug.LogError("[NetworkServiceLocator] INetworkObjectService 未注册。请在游戏启动时调用 Register() 注册网络服务");
                }
                return _objectService;
            }
        }

        public static bool IsInitialized => _playerService != null && _objectService != null;

        public static void Register(INetworkPlayerService playerService, INetworkObjectService objectService)
        {
            _playerService = playerService;
            _objectService = objectService;
            UnityEngine.Debug.Log("[NetworkServiceLocator] 网络服务注册完成");
        }
    }
}