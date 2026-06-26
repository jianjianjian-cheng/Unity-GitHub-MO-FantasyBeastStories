using Domain.Combat.FX;
using Domain.Services;
using Infrastructure.FX.ImpactCannon;
using Infrastructure.Network;
using UnityEngine;

namespace Infrastructure
{
    /// <summary>
    /// Infrastructure 层启动注册器 — 在游戏启动时注册组件工厂和网络服务，供 Domain 层使用
    /// </summary>
    public static class InfrastructureRegistrar
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterFactories()
        {
            // 注册网络服务（玩家身份 + 对象同步）
            NetworkServiceLocator.Register(
                new PhotonPlayerService(),
                new PhotonObjectService()
            );

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

            Debug.Log("[InfrastructureRegistrar] 组件工厂 + 网络服务注册完成");
        }
    }
}