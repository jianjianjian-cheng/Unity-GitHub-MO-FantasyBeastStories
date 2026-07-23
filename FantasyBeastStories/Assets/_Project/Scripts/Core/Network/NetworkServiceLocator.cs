namespace Controllers.Services
{
    public static class NetworkServiceLocator
    {
        private static INetworkPlayerService _playerService;
        private static INetworkObjectService _objectService;
        private static IObjectPoolService _objectPoolService;
        private static IGameActionService _gameActionService;
        private static IControllerRpcService _domainRpcService;

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

        public static IObjectPoolService ObjectPoolService
        {
            get
            {
                if (_objectPoolService == null)
                {
                    UnityEngine.Debug.LogError("[NetworkServiceLocator] IObjectPoolService 未注册。请在游戏启动时调用 RegisterObjectPoolService() 注册对象池服务");
                }
                return _objectPoolService;
            }
        }

        public static IGameActionService GameActionService
        {
            get
            {
                if (_gameActionService == null)
                {
                    UnityEngine.Debug.LogError("[NetworkServiceLocator] IGameActionService 未注册。请在游戏启动时调用 RegisterGameActionService() 注册游戏动作服务");
                }
                return _gameActionService;
            }
        }

        public static IControllerRpcService DomainRpcService
        {
            get
            {
                if (_domainRpcService == null)
                {
                    UnityEngine.Debug.LogError("[NetworkServiceLocator] IControllerRpcService 未注册。请在游戏启动时调用 RegisterDomainRpcService() 注册DomainRPC服务");
                }
                return _domainRpcService;
            }
        }

        public static bool IsInitialized => _playerService != null && _objectService != null;

        public static void Register(INetworkPlayerService playerService, INetworkObjectService objectService)
        {
            _playerService = playerService;
            _objectService = objectService;
            UnityEngine.Debug.Log("[NetworkServiceLocator] 网络服务注册完成");
        }

        public static void RegisterObjectPoolService(IObjectPoolService objectPoolService)
        {
            _objectPoolService = objectPoolService;
            UnityEngine.Debug.Log("[NetworkServiceLocator] IObjectPoolService 注册完成");
        }

        public static void RegisterGameActionService(IGameActionService gameActionService)
        {
            _gameActionService = gameActionService;
            UnityEngine.Debug.Log("[NetworkServiceLocator] IGameActionService 注册完成");
        }

        public static void RegisterDomainRpcService(IControllerRpcService domainRpcService)
        {
            _domainRpcService = domainRpcService;
            UnityEngine.Debug.Log("[NetworkServiceLocator] IControllerRpcService 注册完成");
        }
    }
}