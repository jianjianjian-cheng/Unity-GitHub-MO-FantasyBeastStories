using Controllers.Battle;
using Core;
using UnityEngine;
using Core.Contracts;
using Core.Network;
using Controllers.Network;
using Photon.Pun;
using Core.SharedModel;

namespace Controllers.Experience
{
    /// <summary>
    /// 道具基类 - 继承自DropItemBase，增加效果执行逻辑
    /// 联机模式下使用 itemId（非网络对象）进行同步，与经验球方案二一致
    /// </summary>
    public class PowerUpItemBase : DropItemBase
    {
        [Header("道具配置")]
        [SerializeField] protected PowerUpDataSO powerUpData;

        // 支持多种道具类型的通用方案
        [SerializeField] protected MonoBehaviour effectComponent;
        protected IPowerUpEffect effect;

        /// <summary>道具唯一标识，由房主在生成时分配</summary>
        public uint PowerUpId { get; private set; }

        protected override void OnReachPlayer()
        {
            if (effect == null)
            {
                if (effectComponent != null)
                {
                    effect = effectComponent as IPowerUpEffect;
                    if (effect != null)
                    {
                        Debug.Log($"[PowerUp] 从effectComponent获取到效果: {effect.GetEffectName()}");
                    }
                }

                if (effect == null)
                {
                    effect = GetComponent<IPowerUpEffect>();
                    if (effect == null)
                    {
                        effect = GetComponentInChildren<IPowerUpEffect>();
                        if (effect == null)
                        {
                            Debug.LogError($"[PowerUp] {gameObject.name} 未找到任何实现IPowerUpEffect的组件！");
                            return;
                        }
                        Debug.LogWarning($"[PowerUp] 在子对象中找到效果组件");
                    }
                    Debug.LogWarning($"[PowerUp] 自动获取到效果组件，建议在Inspector中指定effectComponent以获得最佳性能！");
                }
            }

            ExecuteEffect(moveTarget);
            HandleNetworkSync();
            ReturnToPool();
        }

        protected virtual void ExecuteEffect(GameObject player)
        {
            Debug.Log($"[PowerUp] 玩家拾取道具: {powerUpData?.itemName ?? "未知"}");
            effect.Execute(player);

            if (EventChannelLocator.MainContainer != null &&
                EventChannelLocator.MainContainer.powerUpCollectChannel != null)
            {
                EventChannelLocator.MainContainer.powerUpCollectChannel.Raise(
                    new PowerUpCollectEventData
                    {
                        itemName = powerUpData?.itemName ?? "未知",
                        effectName = effect.GetEffectName()
                    }
                );
                Debug.Log($"[PowerUp] 已触发powerUpCollectChannel事件");
            }
            else
            {
                Debug.LogWarning($"[PowerUp] powerUpCollectChannel未初始化，跳过事件触发");
            }
        }

        protected virtual void HandleNetworkSync()
        {
            bool isTest = EventChannelLocator.MainContainer?.gameSettings?.IsTest ?? true;
            if (isTest) return;

            // 广播 itemId 到所有客户端，各客户端隐藏对应的本地道具
            NetworkServiceLocator.ObjectService?.InvokeRPC(
                ManagerRpcBridge.Instance, "RPC_CollectPowerUp",
                NetworkTarget.All, (int)PowerUpId
            );
            Debug.Log($"[PowerUp] 已调用RPC_CollectPowerUp, itemId={PowerUpId}");
        }

        protected virtual void ReturnToPool()
        {
            ServiceLocator.Get<ObjectPoolManager>()?.ReturnToPool(
                PoolConst.PowerUpItem,
                gameObject
            );
        }

        /// <summary>原有 Setup 方法（测试模式 / 不需要网络同步时使用）</summary>
        public void Setup(PowerUpDataSO data)
        {
            powerUpData = data;
            EnsureEffect();
        }

        /// <summary>带 itemId 的 Setup 方法（联机模式由 RPC_SpawnPowerUp 调用）</summary>
        public void SetupWithId(uint id, PowerUpDataSO data)
        {
            PowerUpId = id;
            powerUpData = data;
            EnsureEffect();
        }

        private void EnsureEffect()
        {
            if (effect != null) return;

            if (effectComponent != null)
            {
                effect = effectComponent as IPowerUpEffect;
            }
            if (effect == null)
            {
                effect = GetComponent<IPowerUpEffect>();
                if (effect == null)
                    effect = GetComponentInChildren<IPowerUpEffect>();
            }
        }
    }
}
