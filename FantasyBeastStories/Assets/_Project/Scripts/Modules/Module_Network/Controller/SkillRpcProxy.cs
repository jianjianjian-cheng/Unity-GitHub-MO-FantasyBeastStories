using Controllers.Battle;
using Controllers.Player;
using Controllers.Network;
using Core.Contracts;
using Core.Network;
using Core;
using UnityEngine;

namespace Controllers.Network
{
    /// <summary>
    /// Lua 可调用的统一投射物 RPC 入口（静态类）。
    /// Lua 通过 CS.Controllers.Network.SkillRpcProxy.SpawnProjectile(...) 调用。
    /// 内部查找本地玩家的 CastNetwork 进行网络同步。
    /// </summary>
    public static class SkillRpcProxy
    {
        private static CastNetwork _cachedCastNetwork;

        /// <summary>
        /// 生成投射物（本地 + 网络同步）。
        /// 由 Lua PerformAttack 调用。
        /// </summary>
        public static void SpawnProjectile(
            int templateId,
            Vector3 spawnPos,
            Vector3 direction,
            int targetViewId,
            float damage,
            float critChance,
            float critMultiplier,
            int elementInt,
            bool canSplit,
            int splitCount,
            float splitDamageMultiplier)
        {
            var castNetwork = FindLocalCastNetwork();
            if (castNetwork == null)
            {
                Debug.LogWarning("[SkillRpcProxy] 未找到本地 CastNetwork，无法生成投射物");
                return;
            }

            castNetwork.RequestSpawnProjectile(
                templateId, spawnPos, direction, targetViewId,
                damage, critChance, critMultiplier, elementInt,
                canSplit, splitCount, splitDamageMultiplier
            );
        }

        /// <summary>注册新投射物模板（由 Lua OnStart 调用）</summary>
        public static void RegisterProjectileTemplate(int id, string addressablePath)
        {
            ProjectileTemplateRegistry.Register(id, addressablePath);
        }

        /// <summary>查找本地玩家的 CastNetwork</summary>
        private static CastNetwork FindLocalCastNetwork()
        {
            // 缓存命中检查
            if (_cachedCastNetwork != null && _cachedCastNetwork.photonView != null && _cachedCastNetwork.photonView.IsMine)
                return _cachedCastNetwork;

            // 通过 PlayerManager 查找本地玩家的 CastNetwork
            if (ServiceLocator.Get<PlayerManager>() != null)
            {
                foreach (var playerGo in ServiceLocator.Get<PlayerManager>().ActivePlayerObjects)
                {
                    if (playerGo == null) continue;
                    if (NetworkServiceLocator.PlayerService.IsOwnerOf(playerGo))
                    {
                        _cachedCastNetwork = playerGo.GetComponentInChildren<CastNetwork>();
                        return _cachedCastNetwork;
                    }
                }
            }

            return null;
        }

        /// <summary>清除缓存（场景切换/角色重生时调用）</summary>
        public static void ClearCache()
        {
            _cachedCastNetwork = null;
        }
    }
}