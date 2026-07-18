using Managers;
using Controllers.Combat;
using Core;
using Core.Channels;
using Controllers.Services;
using Controllers.Combat.ImpactCannon;
using Controllers.Network;
using Photon.Pun;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Infrastructure 层启动注册器 — 在游戏启动时注册组件工厂和网络服务，供 Domain 层使用
    /// </summary>
    public static class InfrastructureRegistrar
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterFactories()
        {
            // 全局网络配置，只设置一次（原先在 PlayerStateSync.Awake 中，每个玩家实例都会重复设置）
            PhotonNetwork.SendRate = 30;
            PhotonNetwork.SerializationRate = 30;

            // 注册网络服务（玩家身份 + 对象同步）
            NetworkServiceLocator.Register(
                new PhotonPlayerService(),
                new PhotonObjectService()
            );

            // 预加载事件通道容器并注册到 ServiceLocator
            var container = Resources.Load<EventChannelContainerSO>("EventChannels/MainEventChannels");
            if (container != null)
            {
                ServiceLocator.Register(container);
            }
            else
            {
                Debug.LogError("[InfrastructureRegistrar] 无法加载 MainEventChannels，请在 Resources/EventChannels 目录下创建");
            }

            // 注册早期游戏服务（在 Launcher 加载前，确保 LobbyCanvas 等组件可用）
            GameServiceRegistrar.EnsureRegistered();

            // 注册 ImpactCannon 创建方法
            ComponentFactory.RegisterImpactCannonCreator(obj =>
            {
                var existing = obj.GetComponent<ImpactCannon>();
                if (existing != null)
                    return existing;
                return obj.AddComponent<ImpactCannon>();
            });

            // 注册网络火球发射器创建方法
            ComponentFactory.RegisterNetworkCasterCreator(obj =>
            {
                var existing = obj.GetComponent<CastNetwork>();
                if (existing != null)
                    return existing;
                return obj.AddComponent<CastNetwork>();
            });

            // 启动 PUN 回调桥接器（用于 OnPlayerPropertiesUpdate 等回调转发）
            PhotonCallbackBridge.EnsureExists();

            // 创建 AppRpcBridge — 统一持有 Application 层的 [PunRPC] 方法
            EnsureAppRpcBridge();

            // 创建 DomainRpcBridge — 统一持有 Domain 层的 [PunRPC] 方法
            EnsureDomainRpcBridge();

            // 创建 PresentationRpcBridge — 统一持有 Presentation 层的 [PunRPC] 方法
            EnsurePresentationRpcBridge();

            Debug.Log("[InfrastructureRegistrar] 组件工厂 + 网络服务注册完成");
        }

        private static void EnsureAppRpcBridge()
        {
            var go = new GameObject("AppRpcBridge");
            var pv = go.AddComponent<PhotonView>();
            go.AddComponent<AppRpcBridge>();
            Object.DontDestroyOnLoad(go);
            PhotonCallbackBridge.RegisterBridgeView(pv);
        }

        private static void EnsureDomainRpcBridge()
        {
            var go = new GameObject("DomainRpcBridge");
            var pv = go.AddComponent<PhotonView>();
            go.AddComponent<DomainRpcBridge>();
            Object.DontDestroyOnLoad(go);
            PhotonCallbackBridge.RegisterBridgeView(pv);
        }

        private static void EnsurePresentationRpcBridge()
        {
            var go = new GameObject("PresentationRpcBridge");
            var pv = go.AddComponent<PhotonView>();
            go.AddComponent<PresentationRpcBridge>();
            Object.DontDestroyOnLoad(go);
            PhotonCallbackBridge.RegisterBridgeView(pv);
        }
    }
}