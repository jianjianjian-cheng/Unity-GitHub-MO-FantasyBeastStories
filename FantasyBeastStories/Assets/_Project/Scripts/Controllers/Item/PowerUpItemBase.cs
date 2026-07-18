using Controllers.PowerUp;
using Controllers.PowerUp;
using Core;
using UnityEngine;
using Controllers.PowerUp;
using Core;
using Managers;
using Controllers.Services;
using NetworkTarget = Controllers.Network.NetworkTarget;
using Controllers.Network;
using Photon.Pun;

namespace Controllers.Item
{
    /// <summary>
    /// 道具基类 - 继承自DropItemBase，增加效果执行逻辑
    /// 展示：继承 + 多态 + 组合模式
    /// </summary>
    public class PowerUpItemBase : DropItemBase, IPunObservable
    {
        [Header("道具配置")]
        [SerializeField] protected PowerUpDataSO powerUpData;

        // 支持多种道具类型的通用方案
        [SerializeField] protected MonoBehaviour effectComponent;
        protected IPowerUpEffect effect;

        protected override void OnReachPlayer()
        {
            if (effect == null)
            {
                // 尝试从effectComponent获取
                if (effectComponent != null)
                {
                    effect = effectComponent as IPowerUpEffect;
                    if (effect != null)
                    {
                        Debug.Log($"[PowerUp] 从effectComponent获取到效果: {effect.GetEffectName()}");
                    }
                }

                // 自动查找
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

            // 空值检查
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

            var photonView = gameObject.GetComponent<PhotonView>();
            if (photonView != null)
            {
                NetworkServiceLocator.ObjectService?.InvokeRPC(
                    AppRpcBridge.Instance, "RPC_CollectPowerUp",
                    NetworkTarget.All, photonView.ViewID
                );
                Debug.Log($"[PowerUp] 已调用RPC_CollectPowerUp");
            }
            else
            {
                Debug.LogWarning($"[PowerUp] 未找到PhotonView组件，跳过网络同步");
            }
        }

        protected virtual void ReturnToPool()
        {
            ServiceLocator.Get<ObjectPoolManager>()?.ReturnToPool(
                PoolConst.PowerUpItem,
                gameObject
            );
        }

        public void Setup(PowerUpDataSO data)
        {
            powerUpData = data;

            if (effect == null)
            {
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

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // 发送数据到其他客户端（可选：同步道具状态）
                stream.SendNext(gameObject.activeInHierarchy);
            }
            else
            {
                // 接收来自其他客户端的数据
                bool isActive = (bool)stream.ReceiveNext();
                gameObject.SetActive(isActive);
            }
        }
    }
}