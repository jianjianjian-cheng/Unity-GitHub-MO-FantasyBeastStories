using Controllers.Battle;
using Core.Network;
using Photon.Pun;
using UnityEngine;

namespace Controllers.Network
{
    /// <summary>
    /// Module_Network 启动注册器 — 注册网络服务 + RPC Bridge + 网络火球发射器工厂
    /// BeforeSceneLoad: 注册 PhotonPlayerService + PhotonObjectService
    /// AfterSceneLoad: 创建 RPC Bridge（确保 PhotonCallbackBridge 已创建）
    /// </summary>
    public static class Module_NetworkBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterNetworkServices()
        {
            // 注册网络服务（玩家身份 + 对象同步）
            NetworkServiceLocator.Register(
                new PhotonPlayerService(),
                new PhotonObjectService()
            );

            Debug.Log("[Module_NetworkBootstrap] 网络服务注册完成");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterFactories()
        {
            // 注册网络火球发射器创建方法
            ComponentFactory.RegisterNetworkCasterCreator(obj =>
            {
                var existing = obj.GetComponent<CastNetwork>();
                if (existing != null)
                    return existing;
                return obj.AddComponent<CastNetwork>();
            });

            // 创建 ManagerRpcBridge — 统一持有 Application 层的 [PunRPC] 方法
            EnsureManagerRpcBridge();

            // 创建 ControllerRpcBridge — 统一持有 Domain 层的 [PunRPC] 方法
            EnsureControllerRpcBridge();

            // 创建 UIRpcBridge — 统一持有 Presentation 层的 [PunRPC] 方法
            EnsureUIRpcBridge();

            Debug.Log("[Module_NetworkBootstrap] 网络工厂 + RPC Bridge 注册完成");
        }

        private static void EnsureManagerRpcBridge()
        {
            var go = new GameObject("ManagerRpcBridge");
            var pv = go.AddComponent<PhotonView>();
            go.AddComponent<ManagerRpcBridge>();
            Object.DontDestroyOnLoad(go);
            PhotonCallbackBridge.RegisterBridgeView(pv);
        }

        private static void EnsureControllerRpcBridge()
        {
            var go = new GameObject("ControllerRpcBridge");
            var pv = go.AddComponent<PhotonView>();
            go.AddComponent<ControllerRpcBridge>();
            Object.DontDestroyOnLoad(go);
            PhotonCallbackBridge.RegisterBridgeView(pv);
        }

        private static void EnsureUIRpcBridge()
        {
            var go = new GameObject("UIRpcBridge");
            var pv = go.AddComponent<PhotonView>();
            go.AddComponent<UIRpcBridge>();
            Object.DontDestroyOnLoad(go);
            PhotonCallbackBridge.RegisterBridgeView(pv);
        }
    }
}
