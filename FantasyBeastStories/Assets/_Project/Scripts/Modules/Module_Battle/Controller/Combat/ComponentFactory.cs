using Controllers.Network;
using UnityEngine;

namespace Controllers.Combat
{
    /// <summary>
    /// 组件工厂 — 允许 Domain 层间接创建 Infrastructure 层的组件（绕过 AddComponent<T> 需要具体类型的限制）
    /// 由 Application/Infrastructure 层在启动时注册创建方法
    /// </summary>
    public static class ComponentFactory
    {
        private static System.Func<GameObject, IImpactCannon> _impactCannonCreator;
        private static System.Func<GameObject, INetworkFireballCaster> _networkCasterCreator;
        private static System.Func<GameObject, ProjectileBase> _projectileCreator;

        /// <summary>
        /// 注册 ImpactCannon 创建方法（由 Infrastructure 层调用）
        /// </summary>
        public static void RegisterImpactCannonCreator(System.Func<GameObject, IImpactCannon> creator)
        {
            _impactCannonCreator = creator;
        }

        /// <summary>
        /// 注册网络火球发射器创建方法（由 Infrastructure 层调用）
        /// </summary>
        public static void RegisterNetworkCasterCreator(System.Func<GameObject, INetworkFireballCaster> creator)
        {
            _networkCasterCreator = creator;
        }

        /// <summary>
        /// 在目标 GameObject 上获取或创建 ImpactCannon 组件
        /// </summary>
        public static IImpactCannon GetOrCreateImpactCannon(GameObject obj)
        {
            var existing = obj.GetComponent<IImpactCannon>();
            if (existing != null)
                return existing;

            if (_impactCannonCreator == null)
            {
                Debug.LogError("[ComponentFactory] ImpactCannon creator 未注册！请在启动时调用 ComponentFactory.RegisterImpactCannonCreator");
                return null;
            }

            return _impactCannonCreator(obj);
        }

        /// <summary>
        /// 在目标 GameObject 上获取或创建网络火球发射器
        /// </summary>
        public static INetworkFireballCaster GetOrCreateNetworkCaster(GameObject obj)
        {
            var existing = obj.GetComponent<INetworkFireballCaster>();
            if (existing != null)
                return existing;

            if (_networkCasterCreator == null)
            {
                Debug.LogError("[ComponentFactory] NetworkCaster creator 未注册！请在启动时调用 ComponentFactory.RegisterNetworkCasterCreator");
                return null;
            }

            return _networkCasterCreator(obj);
        }

        /// <summary>
        /// 注册投射物创建方法（由 Infrastructure 层调用）
        /// </summary>
        public static void RegisterProjectileCreator(System.Func<GameObject, ProjectileBase> creator)
        {
            _projectileCreator = creator;
        }

        /// <summary>
        /// 在目标 GameObject 上获取或创建 ProjectileBase 组件
        /// </summary>
        public static ProjectileBase GetOrCreateProjectile(GameObject obj)
        {
            var existing = obj.GetComponent<ProjectileBase>();
            if (existing != null)
                return existing;

            if (_projectileCreator == null)
            {
                Debug.LogError("[ComponentFactory] ProjectileBase creator 未注册！");
                return null;
            }

            return _projectileCreator(obj);
        }
    }
}