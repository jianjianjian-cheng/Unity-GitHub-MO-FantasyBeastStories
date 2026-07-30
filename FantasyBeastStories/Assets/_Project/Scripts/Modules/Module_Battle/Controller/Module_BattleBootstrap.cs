using Controllers.Battle;
using UnityEngine;
using ImpactCannonType = Controllers.Battle.ImpactCannon.ImpactCannon;

namespace Controllers.Battle
{
    /// <summary>
    /// Module_Battle 启动注册器 — 注册战斗相关的组件工厂方法
    /// 在 InfrastructureRegistrar 之后执行（AfterSceneLoad 确保 PhotonCallbackBridge 已创建）
    /// </summary>
    public static class Module_BattleBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterFactories()
        {
            // 注册 ImpactCannon 创建方法
            ComponentFactory.RegisterImpactCannonCreator(obj =>
            {
                var existing = obj.GetComponent<ImpactCannonType>();
                if (existing != null)
                    return existing;
                return obj.AddComponent<ImpactCannonType>();
            });

            Debug.Log("[Module_BattleBootstrap] 组件工厂注册完成");
        }
    }
}
