using Domain.Combat.FX;
using Infrastructure.FX.ImpactCannon;
using Infrastructure.Network;
using UnityEngine;

namespace Infrastructure
{
    /// <summary>
    /// Infrastructure 层启动注册器 — 在游戏启动时注册组件工厂，供 Domain 层使用
    /// </summary>
    public static class InfrastructureRegistrar
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterFactories()
        {
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

            Debug.Log("[InfrastructureRegistrar] 组件工厂注册完成");
        }
    }
}